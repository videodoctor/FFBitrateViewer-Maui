namespace FFBitrateViewer.ApplicationAvalonia.Models.Media;

public record BitRate(int Value) : UInt(Value, Unit.BitsPerSecond);