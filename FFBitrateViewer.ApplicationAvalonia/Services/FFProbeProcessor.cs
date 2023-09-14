using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.Services
{
    public class FFProbeProcessor
    {
        private readonly OSProcessService _oSProcessService;

        private readonly Lazy<string> _ffprobeFilePath;

        public FFProbeProcessor()
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

        public async Task<Version> GetVersion()
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
    }


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
}