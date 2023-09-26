using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.Services
{
    public class OSProcessService
    {

        private readonly Dictionary<Process, (TextWriter, TextWriter, Channel<string>?, Channel<string>?)> _processes = new();

        public async Task<int> ExecuteAsync(
            string command,
            string? workingDirectory = null,
            TextWriter? standardOutputWriter = null,
            TextWriter? standardErrorWriter = null,
            IDictionary<string, string?>? environmentOverrides = null,
            Channel<string>? standardOutputChannel = null,
            Channel<string>? standardErrorChannel = null,
            Encoding? standardOutputEncoding = null,
            Encoding? standardErrorEncoding = null,
            Encoding? standardInputEncoding = null,
            CancellationToken token = default
            )
        {
            standardOutputWriter ??= TextWriter.Null;
            standardErrorWriter ??= TextWriter.Null;
            environmentOverrides ??= new Dictionary<string, string?>();

            var process = GetNewProcessInstance(
                command, 
                workingDirectory,
                standardOutputEncoding,
                standardErrorEncoding,
                standardInputEncoding
            );

            _processes[process] = (standardOutputWriter, standardErrorWriter, standardOutputChannel, standardErrorChannel);
            foreach (var envVar in environmentOverrides)
            { process.StartInfo.Environment[envVar.Key] = environmentOverrides[envVar.Key]; }

            process.OutputDataReceived += OnProcessOutputDataReceived;
            process.ErrorDataReceived += OnProcessErrorDataReceived;

            var hasProcessStarted = process.Start();
            if (!hasProcessStarted)
            {
                throw new OSProcessServiceException($"Failed to execute command: {command}");
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(token);
            _processes.Remove(process);

            process.OutputDataReceived -= OnProcessOutputDataReceived;
            process.ErrorDataReceived -= OnProcessErrorDataReceived;

            standardOutputChannel?.Writer.TryComplete();
            standardErrorChannel?.Writer.TryComplete();

            await standardOutputWriter.FlushAsync();
            await standardErrorWriter.FlushAsync();

            return process.ExitCode;
        }

        public IEnumerable<string> Which(
            string executableFileName,
            params string[] additionalLookupPaths)
        {
            ArgumentException.ThrowIfNullOrEmpty(executableFileName, nameof(executableFileName));

            additionalLookupPaths ??= new string[] { Environment.CurrentDirectory };

            var environmentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

            IEnumerable<string> lookupPaths = environmentPath.Split(Path.PathSeparator);
            lookupPaths = lookupPaths.Concat(additionalLookupPaths);

            foreach (var path in lookupPaths)
            {
                var fullPath = Path.Combine(path, executableFileName);
                if (File.Exists(fullPath))
                {
                    yield return fullPath;
                }
            }
        }

        private async void OnProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = (Process)sender;
            var (_, stdErrWriter, _, stdErrChannel) = _processes[process];
            await WriteReceivedData(e, stdErrWriter, stdErrChannel);
        }

        private static async Task WriteReceivedData(DataReceivedEventArgs dataReceivedEventArgs, TextWriter textWriter, Channel<string>? channel)
        {
            if (dataReceivedEventArgs.Data == null)
            {
                textWriter.Flush();
                return;
            }
            textWriter.Write(dataReceivedEventArgs.Data);

            if (channel != null)
            {
                await channel.Writer.WriteAsync(dataReceivedEventArgs.Data);
            }

        }

        private async void OnProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = (Process)sender;
            var (stdOutWriter, _, stdOutChannel, _) = _processes[process];
            await WriteReceivedData(e, stdOutWriter, stdOutChannel);
        }

        private Process GetNewProcessInstance(
            string command, 
            string?  workingDirectory = null,
            Encoding? standardOutputEncoding = null,
            Encoding? standardErrorEncoding = null,
            Encoding? standardInputEncoding = null
        )
        {
            workingDirectory ??= Environment.CurrentDirectory;

            var process = new Process();
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = standardOutputEncoding;
            process.StartInfo.StandardErrorEncoding = standardErrorEncoding;
            process.StartInfo.StandardInputEncoding = standardInputEncoding;


            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Default Shells (with %SystemRoot% == C:\WINDOWS )
                // %SystemRoot%\System32\cmd.exe /U /C ...
                // %SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe -NoLogo -Mta -NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand -Command ...
                const string PWSH_FILE_NAME = @"powershell.exe";
                const string CMD_FILE_NAME = @"cmd.exe";

                var powershellExeFilePaths = Which(PWSH_FILE_NAME).ToList();
                var cmdExeFilePaths = Which(CMD_FILE_NAME).ToList();
                if (powershellExeFilePaths.Any())
                {
                    process.StartInfo.FileName = powershellExeFilePaths.First();
                    process.StartInfo.ArgumentList.Add("-NoLogo");
                    process.StartInfo.ArgumentList.Add("-Mta");
                    process.StartInfo.ArgumentList.Add("-NoProfile");
                    process.StartInfo.ArgumentList.Add("-NonInteractive");
                    process.StartInfo.ArgumentList.Add("-WindowStyle");
                    process.StartInfo.ArgumentList.Add("Hidden");
                    process.StartInfo.ArgumentList.Add("-EncodedCommand");

                    // NOTE: Appending exit $LASTEXITCODE to the command to get the exit code of the command
                    // https://stackoverflow.com/questions/50200325/returning-an-exit-code-from-a-powershell-script
                    process.StartInfo.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes($"{command}; exit $LASTEXITCODE")));
                }
                else if (cmdExeFilePaths.Any())
                {
                    process.StartInfo.FileName = cmdExeFilePaths.First();
                    process.StartInfo.ArgumentList.Add("/U");
                    process.StartInfo.ArgumentList.Add("/C");
                    process.StartInfo.ArgumentList.Add(command);
                }
                else
                {
                    throw new OSProcessServiceException($"Neither {PWSH_FILE_NAME} or {CMD_FILE_NAME} were not found in PATH.");
                }

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Normally in MacOS default shell is: zsh
                const string ZSH_FILE_NAME = @"zsh";
                var zshFilePaths = Which(ZSH_FILE_NAME).ToList();
                if (zshFilePaths.Any())
                {
                    process.StartInfo.FileName = zshFilePaths.First();
                    process.StartInfo.ArgumentList.Add("-l");
                    process.StartInfo.ArgumentList.Add("-c");
                    process.StartInfo.ArgumentList.Add(command);
                }
                else
                {
                    throw new OSProcessServiceException($"{ZSH_FILE_NAME} was not found in PATH.");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux has many shells:
                //
                //  - Bourne Shell (sh) The Bourne shell was the first default shell on Unix systems, released in 1979. ...
                //  - C Shell (csh) ...
                //  - TENEX C Shell (tcsh) ...
                //  - KornShell (ksh) ...
                //  - Debian Almquist Shell (dash) ...
                //  - Bourne Again Shell (bash) ...
                //  - Z Shell (zsh) ...
                //  - Friendly Interactive Shell (fish)
                //  - Powershell (pwsh)
                //
                // Modern distros already includes sh (Bourne Shell)

                const string SH_FILE_NAME = @"sh";
                var shFilePaths = Which(SH_FILE_NAME).ToList();
                if (shFilePaths.Any())
                {
                    process.StartInfo.FileName = shFilePaths.First();
                    process.StartInfo.ArgumentList.Add("-c");
                    process.StartInfo.ArgumentList.Add(command);
                }
                else
                {
                    throw new OSProcessServiceException($"{SH_FILE_NAME} was not found in PATH.");
                }
            }
            else
            {
                throw new OSProcessServiceException($"Process creation in: {RuntimeInformation.OSDescription} is not supported.");
            }

            return process;
        }
    }



    [Serializable]
    public class OSProcessServiceException : ApplicationException
    {
        public OSProcessServiceException() { }
        public OSProcessServiceException(string message) : base(message) { }
        public OSProcessServiceException(string message, Exception inner) : base(message, inner) { }
        protected OSProcessServiceException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

}