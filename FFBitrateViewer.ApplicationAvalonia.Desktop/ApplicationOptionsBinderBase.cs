using FFBitrateViewer.ApplicationAvalonia.Models.Config;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.IO;
using System.Linq;

namespace FFBitrateViewer.ApplicationAvalonia.Desktop;

public class ApplicationOptionsBinderBase
(
    Option<double?> startTimeAdjustmentOption,
    Option<bool> exitOption,
    Option<bool> logCommandsOption,
    Option<bool> autoRunOption,
    Option<DirectoryInfo> tempDirOption,
    Option<List<FileInfo>> filesOption
)
    : BinderBase<Models.Config.ApplicationOptions>
{

    private readonly Option<double?> _startTimeAdjustmentOption = startTimeAdjustmentOption;

    private readonly Option<bool> _exitOption = exitOption;

    private readonly Option<bool> _logCommandsOption = logCommandsOption;

    private readonly Option<bool> _autoRunOption = autoRunOption;

    private readonly Option<DirectoryInfo> _tempDirOption = tempDirOption;

    private readonly Option<List<FileInfo>> _filesOption = filesOption;

    protected override ApplicationOptions GetBoundValue(BindingContext bindingContext)
        => new ApplicationOptions
        {
            StartTimeAdjustment = bindingContext.ParseResult.GetValueForOption(_startTimeAdjustmentOption),
            Exit = bindingContext.ParseResult.GetValueForOption(_exitOption),
            LogCommands = bindingContext.ParseResult.GetValueForOption(_logCommandsOption),
            AutoRun = bindingContext.ParseResult.GetValueForOption(_autoRunOption),
            TempDir = bindingContext.ParseResult.GetValueForOption(_tempDirOption)!.FullName,
            Files = bindingContext.ParseResult.GetValueForOption(_filesOption)!.Select(fi => fi.FullName).ToList(),
        };
}
