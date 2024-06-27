namespace FFBitrateViewer.ApplicationAvalonia.Models.Media;

public record SampleRate(int Value) : UInt(Value, Unit.Hertz);