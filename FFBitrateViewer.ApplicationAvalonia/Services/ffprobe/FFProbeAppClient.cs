using Sylvan.Data.Csv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.Services.ffprobe
{
    public class FFProbeAppClient
    {
        // ffprobe can produce different output as exlpained in
        // https://ffmpeg.org/ffprobe.html
        // large lists, csv ( see parsers https://www.joelverhagen.com/blog/2020/12/fastest-net-csv-parsers )
        // hierchical info json ( using built in .NET Parser)

        private readonly OSProcessService _oSProcessService;

        private readonly Lazy<string> _ffprobeFilePath;

        public FFProbeAppClient()
        {
            _oSProcessService = new OSProcessService();
            _ffprobeFilePath = new Lazy<string>(WhichFFProbeFilePath);
        }

        private string WhichFFProbeFilePath()
        {
            var ffProbeFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffprobe.exe" : "ffprobe";
            var ffprobeFilePaths = _oSProcessService.Which(ffProbeFileName);
            if (!ffprobeFilePaths.Any())
            {
                throw new FFProbeProcessorException($"Executable {ffProbeFileName} was not found.");
            }
            return ffprobeFilePaths.First();

        }

        public async Task<Version> GetVersionAsync()
        {
            var sb = new StringBuilder();
            using StringWriter sw = new(sb);

            var command = $"{_ffprobeFilePath.Value} -version";
            await _oSProcessService.ExecuteAsync(command, standardOutputWriter: sw);

            var versionText = sb.ToString().Split(" ").Last().Trim();
            if (!Version.TryParse(versionText, out var version))
            {
                throw new FFProbeProcessorException($"Failed extracting ffprobe version with command: {command}");
            }
            return version;
        }

        public async Task<MediaInfo> GetMediaInfoAsync(string mediaFilePath)
        {
            ArgumentException.ThrowIfNullOrEmpty(mediaFilePath);

            if (!File.Exists(mediaFilePath))
            { throw new FileNotFoundException(mediaFilePath); }

            using var memoryStream = new MemoryStream();
            using var streamWriter = new StreamWriter(memoryStream);

            var command = $"{_ffprobeFilePath.Value} -hide_banner -threads 11 -print_format json=compact=1 -loglevel fatal -show_error -show_format -show_streams -show_entries stream_tags=duration {mediaFilePath}";
            await _oSProcessService.ExecuteAsync(command, standardOutputWriter: streamWriter);

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
            var mediaInfo = await JsonSerializer.DeserializeAsync<MediaInfo>(memoryStream, jsonSerializerOptions);

            return mediaInfo!;
        }

        public async IAsyncEnumerable<Packet> GetMediaPackets(string mediaFilePath)
        {
            ArgumentException.ThrowIfNullOrEmpty(mediaFilePath);

            if (!File.Exists(mediaFilePath))
            { throw new FileNotFoundException(mediaFilePath); }

            var commandStdOuputChannel = Channel.CreateUnbounded<string>();

            var command = $"{_ffprobeFilePath.Value} -hide_banner -threads 11 -print_format csv -loglevel fatal -show_error -select_streams v:0 -show_entries packet=dts_time,duration_time,pts_time,size,flags {mediaFilePath}";
            var commandTask = _oSProcessService.ExecuteAsync(command, standardOutputChannel: commandStdOuputChannel);

            await foreach (var todo in commandStdOuputChannel.Reader.ReadAllAsync())
            {
                //        var csvLine = _buffer.ToString();
                //        using var textReader = new StringReader(csvLine);
                //        var csvDataReader = CsvDataReader.Create(textReader);
                //        _buffer = new();
                //        csvDataReader.GetRecords<
                Console.WriteLine($"Completing todo: {todo}");
                yield return new Packet();
            }

            await commandTask;
        }
    }



    #region FFProbeProcessorException
    [Serializable]
    public class FFProbeProcessorException : ApplicationException
    {
        public FFProbeProcessorException() { }
        public FFProbeProcessorException(string message) : base(message) { }
        public FFProbeProcessorException(string message, Exception inner) : base(message, inner) { }
        protected FFProbeProcessorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    #endregion


}