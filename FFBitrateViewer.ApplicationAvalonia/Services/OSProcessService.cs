using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.Services
{
    public class OSProcessService
    {

        private readonly Dictionary<Process, (TextWriter, TextWriter)> _processes = new();

        public async Task ExecuteAsync(
            string command,
            string? workingDirectory = null,
            CancellationToken cancellationToken = default,
            TextWriter? standardOutputWriter = null,
            TextWriter? standardErrorWriter = null,
            IDictionary<string, string?>? environment = null
            )
        {
            standardOutputWriter ??= TextWriter.Null;
            standardErrorWriter ??= TextWriter.Null;
            environment ??= new Dictionary<string, string?>();

            var process = GetNewProcessInstance(command, workingDirectory);

            _processes[process] = (standardOutputWriter, standardErrorWriter);
            foreach (var envVar in environment)
            { process.StartInfo.Environment[envVar.Key] = environment[envVar.Key]; }

            process.OutputDataReceived += OnProcessOutputDataReceived;
            process.ErrorDataReceived += OnProcessErrorDataReceived;

            var hasProcessStarted = process.Start();
            if (!hasProcessStarted)
            {
                throw new OSProcessServiceException($"Failed to execute command: {command}");
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);
            _processes.Remove(process);

            process.OutputDataReceived -= OnProcessOutputDataReceived;
            process.ErrorDataReceived -= OnProcessErrorDataReceived;

            await standardOutputWriter.FlushAsync();
            await standardErrorWriter.FlushAsync();
        }

        public IEnumerable<string> Which(string executableFileName)
        {
            var environmentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

            var paths = environmentPath.Split(Path.PathSeparator);
            paths.Append(Environment.CurrentDirectory);

            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, executableFileName);
                if (File.Exists(fullPath))
                {
                    yield return fullPath;
                }
            }
        }

        private void OnProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = (Process)sender;
            var (_, stdErrWriter) = _processes[process];
            WriteReceivedData(e, stdErrWriter);
        }

        private static void WriteReceivedData(DataReceivedEventArgs dataReceivedEventArgs, TextWriter textWriter)
        {
            if (dataReceivedEventArgs.Data == null)
            {
                textWriter.Flush();
                return;
            }
            textWriter.WriteLine(dataReceivedEventArgs.Data);
        }

        private void OnProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = (Process)sender;
            var (stdOutWriter, _) = _processes[process];
            WriteReceivedData(e, stdOutWriter);
        }

        private Process GetNewProcessInstance(string command, string? workingDirectory = null)
        {
            workingDirectory ??= Environment.CurrentDirectory;

            var process = new Process();
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;


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
                    process.StartInfo.ArgumentList.Add(Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(command)));
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