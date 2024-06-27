namespace FFBitrateViewer.ApplicationAvalonia.Models.Media;

public record UInt(int Value, Unit Unit) : UnitValue<int, Unit>(Value, Unit, Unit.Unknown);