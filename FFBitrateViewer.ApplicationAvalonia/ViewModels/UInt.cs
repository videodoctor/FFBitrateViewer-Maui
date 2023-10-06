namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public record UInt(int Value, Unit Unit) : UnitValue<int, Unit>(Value, Unit, Unit.Unknown);