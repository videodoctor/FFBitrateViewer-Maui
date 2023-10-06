namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public record SampleRate(int Value) : UInt(Value, Unit.Hertz);