using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace FFBitrateViewer.ApplicationAvalonia.Extensibility.AvaloniaUI;

public class SystemDrawingColorToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Drawing.Color drawingColor)
        {
            // Convert System.Drawing.Color to Avalonia.Media.Color
            Avalonia.Media.Color avaloniaColor = Avalonia.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);

            // Create an Avalonia.Media.SolidColorBrush
            return new SolidColorBrush(avaloniaColor);
        }

        return AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(SystemDrawingColorToBrushConverter)} does not support ConvertBack.");
    }
}