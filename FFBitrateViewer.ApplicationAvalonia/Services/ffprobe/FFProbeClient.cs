using Hmb.ProcessRunner;
using Sylvan.Data.Csv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;

/// <summary>
/// FFProbeAppClient is a wrapper for ffprobe command line tool.
/// </summary>
public class FFProbeClient
{
    // ffprobe can produce different output as exlpained in
    // https://ffmpeg.org/ffprobe.html . Thus we use CSV for
    // large lists. The CSV parser is Sylvan.Data.Csv which has the best performance
    // based on benchmark (2020/12) https://www.joelverhagen.com/blog/2020/12/fastest-net-csv-parsers
    // For hierarchical structures with "reasonable" size we use JSON with System.Text.Json parser.

    private readonly ProcessService _processService = new();

    /// <summary>
    /// Returns the full path of ffprobe executable.
    /// </summary>
    public string FFProbeFilePath { get => _fFProbeFilePath ??= WhichFFProbe(); }

    private string? _fFProbeFilePath;

    private string WhichFFProbe()
    {
        var fFProbeFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffprobe.exe" : "ffprobe";
        var fFProbeFilePaths = _processService.Which(fFProbeFileName);
        if (!fFProbeFilePaths.Any())
        {
            throw new FFProbeClientException($"Executable {fFProbeFileName} was not found.");
        }
        return fFProbeFilePaths.First();

    }

    /// <summary>
    /// Returns the version of ffprobe.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="FFProbeClientException"></exception>
    public async Task<Version> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        StringBuilder sb = new();
        using StringWriter sw = new(sb);

        var command = $"{FFProbeFilePath} -version";
        var exitCode = await _processService.ExecuteAsync(command, standardOutputWriter: sw, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (exitCode != 0)
        { throw new FFProbeClientException($"Exit code {exitCode} when executing the following command:{Environment.NewLine}{command}"); }

        var versionText = sb.ToString().Split(" ").Last().Trim();
        if (!Version.TryParse(versionText, out var version))
        {
            throw new FFProbeClientException($"Failed extracting ffprobe version with command: {command}");
        }
        return version;
    }

    /// <summary>
    /// Returns a list of streams for the given media file.
    /// </summary>
    /// <param name="mediaFilePath"></param>
    /// <param name="threadCount"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="FFProbeClientException"></exception>
    public async Task<FFProbeJsonOutput> GetMediaInfoAsync(string mediaFilePath, int threadCount = 11)
    {
        ArgumentException.ThrowIfNullOrEmpty(mediaFilePath);

        if (threadCount <= 0)
        { throw new ArgumentOutOfRangeException(nameof(threadCount)); }

        if (!File.Exists(mediaFilePath))
        { throw new FileNotFoundException(mediaFilePath); }

        var command = $@"{FFProbeFilePath} -hide_banner -threads {threadCount} -print_format json=compact=1 -loglevel fatal -show_error -show_format -show_streams -show_entries stream_tags=duration ""{mediaFilePath}""";

        using var standardOutputMemoryStream = new MemoryStream();
        using var standardOutputWriter = new StreamWriter(standardOutputMemoryStream);
        StringBuilder standardErrorStringBuilder = new StringBuilder();
        StringWriter standardErrorWriter = new StringWriter(standardErrorStringBuilder);
        var exitCode = await _processService.ExecuteAsync(command, standardOutputWriter: standardOutputWriter, standardErrorWriter: standardErrorWriter).ConfigureAwait(false);
        if (exitCode != 0)
        { throw new FFProbeClientException($"Exit code {exitCode} when executing the following command:{Environment.NewLine}{command}.{Environment.NewLine}Standard Error Output: '{standardErrorStringBuilder}'"); }

#if DEBUG
        standardOutputMemoryStream.Seek(0, SeekOrigin.Begin);
        var jsonText = Encoding.UTF8.GetString(standardOutputMemoryStream.ToArray());
        Debug.WriteLine(jsonText);
#endif

        standardOutputMemoryStream.Seek(0, SeekOrigin.Begin);


        var jsonSerializerOptions = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
        var mediaInfo = await JsonSerializer.DeserializeAsync<FFProbeJsonOutput>(standardOutputMemoryStream, jsonSerializerOptions).ConfigureAwait(false);

        return mediaInfo!;
    }

    /// <summary>
    /// Returns a list of packets for the given media file.
    /// </summary>
    /// <param name="mediaFilePath"></param>
    /// <param name="streamId"></param>
    /// <param name="threadCount"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="FFProbeClientException"></exception>
    public async Task GetProbePackets(
        Channel<FFProbePacket> probePacketChannel,
        string mediaFilePath,
        int streamId = 0,
        int threadCount = 11
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(mediaFilePath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threadCount);

        if (!File.Exists(mediaFilePath))
        { throw new FileNotFoundException(mediaFilePath); }

        var commandStdOutputChannel = Channel.CreateUnbounded<string>();

        var producer = Task.Run(async () =>
        {
            var command = $@"{FFProbeFilePath} -hide_banner -threads {threadCount} -print_format csv -loglevel fatal -show_error -select_streams v:{streamId} -show_entries packet=dts_time,duration_time,pts_time,size,flags ""{mediaFilePath}""";
            var exitCode = await _processService.ExecuteAsync(command, standardOutputChannel: commandStdOutputChannel).ConfigureAwait(false);
            commandStdOutputChannel.Writer.TryComplete();
            if (exitCode != 0)
            { throw new FFProbeClientException($"Exit code {exitCode} when executing the following command:{Environment.NewLine}{command}"); }
        });

        var consumer = Task.Run(async () =>
        {

            var csvDataReaderOptions = new CsvDataReaderOptions
            { HasHeaders = false, };


            // NOTE: Because of command output can be quite large.
            //       We use Publisher/Consumer pattern thru System.Threading.Channel
            await foreach (var csvLine in commandStdOutputChannel.Reader.ReadAllAsync())
            {
                // Converts a CSV line to a Packet instance. Following is a sample line:
                // [CSV format]
                // packet,0.000000,N/A,0.016000,1186,K__
                // [indexesfor the reader]
                // 0      1                 2            3                      4          5
                using var textReader = new StringReader(csvLine);
                var csvDataReader = CsvDataReader.Create(textReader, csvDataReaderOptions);
                await csvDataReader.ReadAsync().ConfigureAwait(false);

                var entryType = csvDataReader.GetString(0);
                if (string.Compare(entryType, "packet", true) != 0)
                {
                    throw new FFProbeClientException($"Entry Type:{entryType} is not supported");
                }
                //Index     Sample(csv)     ffprobe (Frame)
                //0         packet,         
                //1         0.000000,       PTSTime (Frame:StartTime)
                //2         N / A,          
                //3         0.016000,       DuratonTime (Frame:Duration)
                //4         1186,           Size (Frame:Size)
                //5         K__             Flag (Frame:Flags)

                var probePacket = new FFProbePacket
                (
                    // from CSV
                    PTSTime: double.TryParse(csvDataReader.GetString(1), out var pstTime) ? pstTime : default,
                    // csvDataReader.GetString(2)
                    DurationTime: double.TryParse(csvDataReader.GetString(3), out var durationTime) ? durationTime : default,
                    Size: int.TryParse(csvDataReader.GetString(4), out var size) ? size : default,
                    Flags: csvDataReader.GetString(5),

                    // default
                    CodecType: default,
                    DTS: default,
                    DTSTime: default,
                    Duration: default,
                    PTS: default,
                    StreamIndex: default
                );
                
                await probePacketChannel.Writer.WriteAsync(probePacket).ConfigureAwait(false);

            }

            probePacketChannel.Writer.TryComplete();

        });

        await Task.WhenAll(producer, consumer);


    }
}