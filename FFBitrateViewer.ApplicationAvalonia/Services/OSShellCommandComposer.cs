using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FFBitrateViewer.ApplicationAvalonia.Services
{
    public abstract record OSShellCommandComposer
    {
        public abstract (string executable, List<string> arguments) GetCommandLine(string command);

        public static IEnumerable<OSShellCommandComposer> GetCommandComposers()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return new PowerShellCommandComposer();
                yield return new CmdShellCommandComposer();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                yield return new BourneShellCommandComposer();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                yield return new ZShellCommandComposer();
            }
            else
            {
                throw new OSProcessServiceException($"Unsupported OS: {RuntimeInformation.OSDescription}");
            }
        }
    }

    internal sealed record CmdShellCommandComposer : OSShellCommandComposer
    {
        // Default Shells (with %SystemRoot% == C:\WINDOWS )
        // %SystemRoot%\System32\cmd.exe /U /C ...
        public override (string executable, List<string> arguments) GetCommandLine(string command)
            => ("cmd.exe", new List<string> {
                "/U",
                "/C",
                command
            });
    }

    internal sealed record PowerShellCommandComposer : OSShellCommandComposer
    {
        // Default Shells (with %SystemRoot% == C:\WINDOWS )
        // %SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe -NoLogo -Mta -NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand -Command ...
        public override (string executable, List<string> arguments) GetCommandLine(string command)
            => ("powershell.exe", new List<string> {
                "-NoLogo",
                "-Mta",
                "-NoProfile",
                "-NonInteractive",
                "-WindowStyle", "Hidden",
                "-EncodedCommand",
                // NOTE: Appending exit $LASTEXITCODE to the command to get the exit code of the command
                // https://stackoverflow.com/questions/50200325/returning-an-exit-code-from-a-powershell-script
                Convert.ToBase64String(Encoding.Unicode.GetBytes($"{command}; exit $LASTEXITCODE"))
            });
    }

    internal sealed record ZShellCommandComposer : OSShellCommandComposer
    {
        // Normally in MacOS default shell is: zsh
        public override (string executable, List<string> arguments) GetCommandLine(string command)
            => ("zsh", new List<string> {
                "-l",
                "-c",
                command
            });
    }

    internal sealed record BourneShellCommandComposer : OSShellCommandComposer
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
        public override (string executable, List<string> arguments) GetCommandLine(string command)
            => ("sh", new List<string> {
                "-c",
                command
            });
    }

}