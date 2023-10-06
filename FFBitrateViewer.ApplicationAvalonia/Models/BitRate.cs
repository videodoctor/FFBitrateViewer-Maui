namespace FFBitrateViewer.ApplicationAvalonia.Models;

public record BitRate(int Value) : UInt(Value, Unit.BitsPerSecond);