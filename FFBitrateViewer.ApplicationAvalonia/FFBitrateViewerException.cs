using System;

namespace FFBitrateViewer.ApplicationAvalonia;

[Serializable]
public class FFBitrateViewerException : System.ApplicationException
{
    public FFBitrateViewerException() { }
    public FFBitrateViewerException(string message) : base(message) { }
    public FFBitrateViewerException(string message, Exception inner) : base(message, inner) { }
}