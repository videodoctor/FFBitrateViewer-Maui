using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.Services
{
    public class OSProcessService
    {

        private readonly Dictionary<Process, (TextWriter, TextWriter)> _processes = new Dictionary<Process, (TextWriter, TextWriter)>();

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

            await standardOutputWriter.FlushAsync();
            await standardErrorWriter.FlushAsync();

            process.OutputDataReceived -= OnProcessOutputDataReceived;
            process.ErrorDataReceived -= OnProcessErrorDataReceived;

        }

        private void OnProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = (Process)sender;
            var (_, stdErrWriter) = _processes[process];
            if (e.Data == null)
            {
                stdErrWriter.Flush();
                return;
            }
            stdErrWriter.WriteLine(e.Data);
        }

        private void OnProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = (Process)sender;
            var (stdOutWriter, _) = _processes[process];
            if (e.Data == null)
            {
                stdOutWriter.Flush();
                return;
            }
            stdOutWriter.WriteLine(e.Data);
        }

        public static Process GetNewProcessInstance(string command, string? workingDirectory = null)
        {
            workingDirectory ??= Environment.CurrentDirectory;

            var process = new Process();
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            // TODO: Implement Which(string executableFileName) method that performs a look up shell using entries from PATH, returns (enum Shell, string path)
            // [Flags] private enum Shell : int { Cmd = 1, Pwsh = 2, Sh = 4, Bash = 8, Zsh = 16}

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Default Shells (with %SystemRoot% == C:\WINDOWS )
                // %SystemRoot%\System32\cmd.exe /U /C ...
                // %SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe -NoLogo -Mta -NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand -Command ...
                const string PWSH_FILE_PATH = @"C:\WINDOWS\System32\WindowsPowerShell\v1.0\powershell.exe";
                const string CMD_FILE_PATH = @"C:\WINDOWS\System32\cmd.exe";

                if (System.IO.File.Exists(PWSH_FILE_PATH))
                {
                    process.StartInfo.FileName = PWSH_FILE_PATH;
                    process.StartInfo.ArgumentList.Add("-NoLogo");
                    process.StartInfo.ArgumentList.Add("-Mta");
                    process.StartInfo.ArgumentList.Add("-NoProfile");
                    process.StartInfo.ArgumentList.Add("-NonInteractive");
                    process.StartInfo.ArgumentList.Add("-WindowStyle");
                    process.StartInfo.ArgumentList.Add("Hidden");
                    process.StartInfo.ArgumentList.Add("-EncodedCommand");
                    process.StartInfo.ArgumentList.Add(Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(command)));
                }
                else if (System.IO.File.Exists(CMD_FILE_PATH))
                {
                    process.StartInfo.FileName = CMD_FILE_PATH;
                    process.StartInfo.ArgumentList.Add("/U");
                    process.StartInfo.ArgumentList.Add("/C");
                    process.StartInfo.ArgumentList.Add(command);
                }
                else
                {
                    throw new OSProcessServiceException(string.Join(string.Empty,
                        "Failed to find a compatible shell. Searched for: " + Environment.NewLine,
                        "   - ", PWSH_FILE_PATH, Environment.NewLine,
                        "   - ", CMD_FILE_PATH
                    ));
                }

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Default Shell zsh
                // /usr/bin/zsh
                const string ZSH_FILE_PATH = @"/usr/bin/zsh";

                if (System.IO.File.Exists(ZSH_FILE_PATH))
                {
                    process.StartInfo.FileName = ZSH_FILE_PATH;
                    process.StartInfo.ArgumentList.Add("-c");
                    process.StartInfo.ArgumentList.Add(command);
                }
                else
                {
                    throw new OSProcessServiceException(string.Join(string.Empty,
                        "Failed to find a compatible shell. Searched for: " + Environment.NewLine,
                        "   - ", ZSH_FILE_PATH
                    ));
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
                // Modern distros use /bin/sh as an alias for the default shell

                const string SH_FILE_PATH = @"/usr/bin/sh";
                if (!System.IO.File.Exists(SH_FILE_PATH))
                {
                    process.StartInfo.FileName = SH_FILE_PATH;
                    process.StartInfo.ArgumentList.Add("-c");
                    process.StartInfo.ArgumentList.Add(command);
                }
                else
                {
                    throw new OSProcessServiceException(string.Join(string.Empty,
                        "Failed to find a compatible shell. Searched for: " + Environment.NewLine,
                        "   - ", SH_FILE_PATH
                    ));
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