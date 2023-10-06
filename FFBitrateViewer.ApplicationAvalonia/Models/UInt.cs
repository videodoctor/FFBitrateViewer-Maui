namespace FFBitrateViewer.ApplicationAvalonia.Models;

public record UInt(int Value, Unit Unit) : UnitValue<int, Unit>(Value, Unit, Unit.Unknown);