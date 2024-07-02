using FFBitrateViewer.ApplicationAvalonia.Models.Media;
using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;
using System;
using System.Linq;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public interface IPlotStrategy
{
    PlotViewType PlotViewType { get; }

    string AxisYTickLabelSuffix { get; }

    string AxisYTickLabelPrefix { get; }

    string AxisYLegendTitle { get; }

    (double? X, double Y) GetDataPoint(double startTime, FFProbePacket probePacket);

    double? GetAxisYForFile(FileItemViewModel file);

    string AxisXValueToString(double duration) => TimeSpan.FromSeconds(duration).ToString("g");

    string AxisYValueToString(double value) => $"{value:n}";

    string AxisXTickLabelSuffix => string.Empty;

    string AxisXTickLabelPrefix => "Time";

    string AxisXLegendTitle => "";

}


public class FrameBasedPlotStrategy : IPlotStrategy
{
    public PlotViewType PlotViewType => PlotViewType.FrameBased;

    public string AxisYTickLabelSuffix => "kb";

    public string AxisYTickLabelPrefix => "Frame size";

    public string AxisYLegendTitle => "Frame size [kb]";

    public double? GetAxisYForFile(FileItemViewModel file)
        => file.Frames.Max(f => f.Size);

    public (double? X, double Y) GetDataPoint(double startTime, FFProbePacket probePacket)
        => ((probePacket.PTSTime ?? 0) - startTime, Convert.ToDouble(probePacket.Size) / 1000.0);
}

public class SecondBasedPlotStrategy : IPlotStrategy
{
    public PlotViewType PlotViewType => PlotViewType.SecondBased;

    public string AxisYTickLabelSuffix => "kb/s";

    public string AxisYTickLabelPrefix => "Bit rate";

    public string AxisYLegendTitle => "Bit rate [kb/s]";

    public double? GetAxisYForFile(FileItemViewModel file)
        => file.GetBitRateMaximum(magnitudeOrder: 1000);

    public (double? X, double Y) GetDataPoint(double startTime, FFProbePacket probePacket)
        => ((probePacket.PTSTime ?? 0) - startTime, Convert.ToDouble(probePacket.BitRate) / 1000.0);
}

public class GOPBasedPlotStrategy : IPlotStrategy
{
    public PlotViewType PlotViewType => PlotViewType.GOPBased;

    public string AxisYTickLabelSuffix => "kb/GOP";

    public string AxisYTickLabelPrefix => "Bit rate";

    public string AxisYLegendTitle => "Bit rate [kb/GOP]";

    public double? GetAxisYForFile(FileItemViewModel file)
    {
        throw new NotImplementedException();
    }

    public (double? X, double Y) GetDataPoint(double startTime, FFProbePacket probePacket)
    {
        throw new NotImplementedException();
    }
}

internal class NonePlotStrategy : IPlotStrategy
{
    public static IPlotStrategy Instance => _lazyPlotStrategy.Value;

    private static readonly Lazy<IPlotStrategy> _lazyPlotStrategy = new Lazy<IPlotStrategy>(() => new NonePlotStrategy());

    private NonePlotStrategy()
    { }

    public PlotViewType PlotViewType => PlotViewType.FrameBased;

    public string AxisYTickLabelSuffix => string.Empty;

    public string AxisYTickLabelPrefix => string.Empty;

    public string AxisYLegendTitle => string.Empty;

    public double? GetAxisYForFile(FileItemViewModel file)
    { return 0.0; }

    public (double? X, double Y) GetDataPoint(double startTime, FFProbePacket probePacket)
    { return (0.0, 0.0); }
}