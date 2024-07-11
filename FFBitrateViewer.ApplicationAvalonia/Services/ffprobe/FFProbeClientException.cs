using System;

namespace FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;

[Serializable]
public class FFProbeClientException : FFBitrateViewerException
{
    public FFProbeClientException() { }
    public FFProbeClientException(string message) : base(message) { }
    public FFProbeClientException(string message, Exception inner) : base(message, inner) { }
}