using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia;
using System;
using System.Globalization;

namespace FFBitrateViewer.ApplicationAvalonia.Extensibility.AvaloniaUI;

public class ScottPlotColorToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ScottPlot.Color scottPlotColor)
        {
            // Convert ScottPlot.Color to System.Drawing.Color
            System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(scottPlotColor.A, scottPlotColor.R, scottPlotColor.G, scottPlotColor.B);

            // Convert System.Drawing.Color to Avalonia.Media.Color
            Avalonia.Media.Color avaloniaColor = Avalonia.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);

            // Create an Avalonia.Media.SolidColorBrush
            return new SolidColorBrush(avaloniaColor);
        }

        return AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ScottPlotColorToBrushConverter does not support ConvertBack.");
    }
}