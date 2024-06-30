using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using FFBitrateViewer.ApplicationAvalonia.Models.Config;
using FFBitrateViewer.ApplicationAvalonia.Models.Media;

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

        var plotViewTypeOption = new Option<PlotViewType>("--PlotViewType", getDefaultValue: () => PlotViewType.FrameBased, "The kind of plot view selected by default");
        plotViewTypeOption.AddAlias("-p");

        var rootCommand = new RootCommand("Visualizes video bitrate received by ffprobe.exe")
        {
            startTimeAdjustmentOption,
            exitOption,
            logCommandsOption,
            autoRunOption,
            tempDirOption,
            filesOption,
            plotViewTypeOption,
        };

        ApplicationOptionsBinderBase applicationOptionsBinderBase =  new (startTimeAdjustmentOption, exitOption, logCommandsOption, autoRunOption, tempDirOption, filesOption, plotViewTypeOption);
        rootCommand.SetHandler((applicationOptions) =>
        {
            BuildAvaloniaApp(applicationOptions)
            .StartWithClassicDesktopLifetime(Environment.GetCommandLineArgs());
        }, applicationOptionsBinderBase);

        return await rootCommand.InvokeAsync(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => BuildAvaloniaApp(default);

    public static AppBuilder BuildAvaloniaApp(ApplicationOptions? applicationOptions)
        => AppBuilder.Configure(() => new App() { ApplicationOptions = applicationOptions })
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

}
