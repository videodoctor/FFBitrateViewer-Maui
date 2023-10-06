namespace FFBitrateViewer.ApplicationAvalonia.Models;

public record SampleRate(int Value) : UInt(Value, Unit.Hertz);