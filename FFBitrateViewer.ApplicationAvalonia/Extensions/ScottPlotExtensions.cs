using Avalonia.Media;
using System.Runtime.CompilerServices;

namespace FFBitrateViewer.ApplicationAvalonia.Extensions;

public static class ScottPlotExtensions
{

    public static IBrush ToAvaloniaBrush(this ScottPlot.Color scottPlotColor)
    {
        // Convert ScottPlot.Color to System.Drawing.Color
        System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(scottPlotColor.A, scottPlotColor.R, scottPlotColor.G, scottPlotColor.B);

        // Convert System.Drawing.Color to Avalonia.Media.Color
        Avalonia.Media.Color avaloniaColor = Avalonia.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);

        // Create an Avalonia.Media.SolidColorBrush
        IBrush avaloniaBrush = new SolidColorBrush(avaloniaColor);

        return avaloniaBrush;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Drawing.Color ToDrawingColor(this ScottPlot.Color scottPlotColor)
        => ScottPlot.Color.ToColor(scottPlotColor);

}
