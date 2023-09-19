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

namespace FFBitrateViewer.ApplicationAvalonia.Services.ffprobe
{
    public class FFProbeAppClient
    {
        // ffprobe can produce different output as exlpained in
        // https://ffmpeg.org/ffprobe.html . Thus we use CSV for
        // large lists. The CSV parser is Sylvan.Data.Csv which has the best performance
        // based on benchmark (2020/12) https://www.joelverhagen.com/blog/2020/12/fastest-net-csv-parsers
        // For hierarchical structures with "reasonable" size we use JSON with System.Text.Json parser.

        private readonly OSProcessService _oSProcessService = new();


        public string FFProbeFilePath { get => _ffprobeFilePath ??= WhichFFProbeFilePath(); }
        private string? _ffprobeFilePath;


        private string WhichFFProbeFilePath()
        {
            var ffProbeFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffprobe.exe" : "ffprobe";
            var ffprobeFilePaths = _oSProcessService.Which(ffProbeFileName);
            if (!ffprobeFilePaths.Any())
            {
                throw new FFProbeAppClientException($"Executable {ffProbeFileName} was not found.");
            }
            return ffprobeFilePaths.First();

        }

        public async Task<Version> GetVersionAsync()
        {
            StringBuilder sb = new();
            using StringWriter sw = new(sb);

            var command = $"{FFProbeFilePath} -version";
            var exitCode = await _oSProcessService.ExecuteAsync(command, standardOutputWriter: sw);
            if (exitCode != 0)
            { throw new FFProbeAppClientException($"Exit code {exitCode} when executing the following command:{Environment.NewLine}{command}"); }

            var versionText = sb.ToString().Split(" ").Last().Trim();
            if (!Version.TryParse(versionText, out var version))
            {
                throw new FFProbeAppClientException($"Failed extracting ffprobe version with command: {command}");
            }
            return version;
        }

        public async Task<FFProbeJsonOutput> GetMediaInfoAsync(string mediaFilePath, int threadCount = 11)
        {
            ArgumentException.ThrowIfNullOrEmpty(mediaFilePath);

            if (threadCount <= 0)
            { throw new ArgumentOutOfRangeException(nameof(threadCount)); }

            if (!File.Exists(mediaFilePath))
            { throw new FileNotFoundException(mediaFilePath); }

            var command = $@"{FFProbeFilePath} -hide_banner -threads {threadCount} -print_format json=compact=1 -loglevel fatal -show_error -show_format -show_streams -show_entries stream_tags=duration ""{mediaFilePath}""";

            using var memoryStream = new MemoryStream();
            using var streamWriter = new StreamWriter(memoryStream);
            var exitCode = await _oSProcessService.ExecuteAsync(command, standardOutputWriter: streamWriter);
            if (exitCode != 0)
            { throw new FFProbeAppClientException($"Exit code {exitCode} when executing the following command:{Environment.NewLine}{command}"); }

#if DEBUG
            memoryStream.Seek(0, SeekOrigin.Begin);
            var jsonText = Encoding.UTF8.GetString(memoryStream.ToArray());
            Debug.WriteLine(jsonText);
#endif

            memoryStream.Seek(0, SeekOrigin.Begin);


            var jsonSerializerOptions = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
            var mediaInfo = await JsonSerializer.DeserializeAsync<FFProbeJsonOutput>(memoryStream, jsonSerializerOptions);

            return mediaInfo!;
        }

        public async IAsyncEnumerable<FFProbePacket> GetProbePackets(
            string mediaFilePath,
            int streamId = 0,
            int threadCount = 11,
            [EnumeratorCancellation] CancellationToken token = default
        )
        {
            ArgumentException.ThrowIfNullOrEmpty(mediaFilePath);

            if (threadCount <= 0)
            { throw new ArgumentOutOfRangeException(nameof(threadCount)); }

            if (!File.Exists(mediaFilePath))
            { throw new FileNotFoundException(mediaFilePath); }

            var commandStdOuputChannel = Channel.CreateUnbounded<string>();
            var command = $@"{FFProbeFilePath} -hide_banner -threads {threadCount} -print_format csv -loglevel fatal -show_error -select_streams v:{streamId} -show_entries packet=dts_time,duration_time,pts_time,size,flags ""{mediaFilePath}""";
            var commandTask = _oSProcessService.ExecuteAsync(command, standardOutputChannel: commandStdOuputChannel, token: token);

            var csvDataReaderOptions = new CsvDataReaderOptions
            { HasHeaders = false, };

            // NOTE: Because of command output can be quite large.
            //       We use Publisher/Consumer pattern thru System.Threading.Channel
            await foreach (var csvLine in commandStdOuputChannel.Reader.ReadAllAsync(token))
            {
                // Converts a CSV line to a Packet instance. Following is a sample line:
                // [CSV format]
                // packet,0.088000,N/A,0.033000,15368,K__
                // [COMPAT format]
                // packet|pts_time=0.088000|dts_time=N/A|duration_time=0.033000|size=15368|flags=K__
                // [indexesfor the reader]
                // 0      1                 2            3                      4          5
                using var textReader = new StringReader(csvLine);
                var csvDataReader = CsvDataReader.Create(textReader, csvDataReaderOptions);
                await csvDataReader.ReadAsync(token);

                var entryType = csvDataReader.GetString(0);
                if (string.Compare(entryType, "packet", true) != 0)
                {
                    throw new FFProbeAppClientException($"Entry Type:{entryType} is not supported");
                }

                yield return new FFProbePacket
                (
                    CodecType: default,
                    DTS: default,
                    DTSTime: double.TryParse(csvDataReader.GetString(2), out var dtsTime) ? dtsTime : default,
                    Duration: default,
                    DurationTime: double.TryParse(csvDataReader.GetString(3), out var durationTime) ? durationTime : default,
                    Flags: csvDataReader.GetString(5),
                    PTS: default,
                    PTSTime: double.TryParse(csvDataReader.GetString(1), out var pstTime) ? pstTime : default,
                    Size: int.TryParse(csvDataReader.GetString(4), out var size) ? size : default,
                    StreamIndex: default
                );
            }

            var exitCode = await commandTask;
            if (exitCode != 0)
            { throw new FFProbeAppClientException($"Exit code {exitCode} when executing the following command:{Environment.NewLine}{command}"); }
        }
    }



    #region FFProbeProcessorException
    [Serializable]
    public class FFProbeAppClientException : ApplicationException
    {
        public FFProbeAppClientException() { }
        public FFProbeAppClientException(string message) : base(message) { }
        public FFProbeAppClientException(string message, Exception inner) : base(message, inner) { }
        protected FFProbeAppClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    #endregion


}