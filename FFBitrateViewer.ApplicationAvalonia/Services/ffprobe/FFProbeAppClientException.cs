using System;

namespace FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;

[Serializable]
public class FFProbeAppClientException : ApplicationException
{
    public FFProbeAppClientException() { }
    public FFProbeAppClientException(string message) : base(message) { }
    public FFProbeAppClientException(string message, Exception inner) : base(message, inner) { }
    protected FFProbeAppClientException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
