using FFBitrateViewer.ApplicationAvalonia.Models.Media;
using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;
using System;
using System.Linq;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public interface IPlotStrategy
{
    PlotViewType PlotViewType { get; }

    string AxisYTickLabelSuffix { get; }

    string AxisYTitleLabel { get; }

    (double? X, double Y) GetDataPoint(double startTime, FFProbePacket probePacket);

    double? GetAxisYForFile(FileItemViewModel file);

}


public class FrameBasedPlotStrategy : IPlotStrategy
{
    public PlotViewType PlotViewType => PlotViewType.FrameBased;

    public string AxisYTickLabelSuffix => "kb";

    public string AxisYTitleLabel => "Frame size [kb]";

    public double? GetAxisYForFile(FileItemViewModel file)
        => file.Frames.Max(f => f.Size);

    public (double? X, double Y) GetDataPoint(double startTime, FFProbePacket probePacket)
        => ((probePacket.PTSTime ?? 0) - startTime, Convert.ToDouble(probePacket.Size) / 1000.0);
}

public class SecondBasedPlotStrategy : IPlotStrategy
{
    public PlotViewType PlotViewType => PlotViewType.SecondBased;

    public string AxisYTickLabelSuffix => "kb/s";

    public string AxisYTitleLabel => "Bit rate [kb/s]";

    public double? GetAxisYForFile(FileItemViewModel file)
        => file.GetBitRateMaximum(magnitudeOrder: 1000);

    public (double? X, double Y) GetDataPoint(double startTime, FFProbePacket probePacket)
        => ((probePacket.PTSTime ?? 0) - startTime, Convert.ToDouble(probePacket.BitRate) / 1000.0);
}

public class GOPBasedPlotStrategy : IPlotStrategy
{
    public PlotViewType PlotViewType => PlotViewType.GOPBased;

    public string AxisYTickLabelSuffix => "kb/GOP";

    public string AxisYTitleLabel => "Bit rate";

    public double? GetAxisYForFile(FileItemViewModel file)
    {
        throw new NotImplementedException();
    }

    public (double? X, double Y) GetDataPoint(double startTime, FFProbePacket probePacket)
    {
        throw new NotImplementedException();
    }
}