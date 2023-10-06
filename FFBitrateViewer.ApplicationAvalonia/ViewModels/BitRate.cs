namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public record BitRate(int Value) : UInt(Value, Unit.BitsPerSecond);