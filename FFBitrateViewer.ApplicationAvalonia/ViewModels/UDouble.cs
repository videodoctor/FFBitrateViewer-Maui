namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public record UDouble(double Value, Unit Unit) : UnitValue<double, Unit>(Value, Unit, Unit.Unknown);