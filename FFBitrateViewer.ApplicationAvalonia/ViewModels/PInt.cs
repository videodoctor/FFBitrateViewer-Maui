namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public record PInt(int X, int Y)
{
    public string ToString(char separator)
    {
        return string.Concat(X, separator, Y);
    }
}