using System;

namespace FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;

[Serializable]
public class FFProbeClientException : ApplicationException
{
    public FFProbeClientException() { }
    public FFProbeClientException(string message) : base(message) { }
    public FFProbeClientException(string message, Exception inner) : base(message, inner) { }
}
