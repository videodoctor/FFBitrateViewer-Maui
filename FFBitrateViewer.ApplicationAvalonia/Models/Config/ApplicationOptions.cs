using FFBitrateViewer.ApplicationAvalonia.Models.Media;
using System.Collections.Generic;

namespace FFBitrateViewer.ApplicationAvalonia.Models.Config;

public class ApplicationOptions
{
    public double? StartTimeAdjustment { get; set; }
    public bool Exit { get; set; }
    public bool LogCommands { get; set; }
    public bool AutoRun { get; set; }
    public string TempDir { get; set; } = string.Empty;
    public List<string> Files { get; set; } = [];
    public PlotViewType PlotView { get; set; }
    public string FFProbeFilePath { get; set; } = string.Empty;
}