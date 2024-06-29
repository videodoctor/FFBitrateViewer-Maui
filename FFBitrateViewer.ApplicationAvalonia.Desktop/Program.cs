using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using FFBitrateViewer.ApplicationAvalonia.Models.Config;

namespace FFBitrateViewer.ApplicationAvalonia.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    private static async Task<int> Main(string[] args)
    {
        var startTimeAdjustmentOption = new Option<double?>("--StartTimeAdjustment", getDefaultValue: () => null, "Time adjustment when compting the plot");
        startTimeAdjustmentOption.AddAlias("-s");

        var exitOption = new Option<bool>("--Exit", getDefaultValue: () => false);
        exitOption.AddAlias("-e");

        var logCommandsOption = new Option<bool>("--LogCommands", getDefaultValue: () => false, "Whether or not to log executed commands");
        logCommandsOption.AddAlias("-l");

        var autoRunOption = new Option<bool>("--AutoRun", getDefaultValue: () => false, "Whether or not start automatically the file processing.");
        autoRunOption.AddAlias("-a");

        var tempDirOption = new Option<DirectoryInfo>("--TempDir", getDefaultValue: () => new DirectoryInfo(Path.GetTempPath()), "Temporary directory");
        tempDirOption.AddAlias("-t");

        var filesOption = new Option<List<FileInfo>>("--Files", getDefaultValue: () => [], "Input files");
        filesOption.AddAlias("-f");

        var rootCommand = new RootCommand("Visualizes video bitrate received by ffprobe.exe")
        {
            startTimeAdjustmentOption,
            exitOption,
            logCommandsOption,
            autoRunOption,
            tempDirOption,
            filesOption,
        };

        ApplicationOptionsBinderBase applicationOptionsBinderBase =  new (startTimeAdjustmentOption, exitOption, logCommandsOption, autoRunOption, tempDirOption, filesOption);
        rootCommand.SetHandler((applicationOptions) =>
        {
            BuildAvaloniaApp(applicationOptions)
            .StartWithClassicDesktopLifetime(Environment.GetCommandLineArgs());
        }, applicationOptionsBinderBase);

        return await rootCommand.InvokeAsync(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(ApplicationOptions? applicationOptions = null)
        => AppBuilder.Configure(() => new App() { ApplicationOptions = applicationOptions })
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

}
