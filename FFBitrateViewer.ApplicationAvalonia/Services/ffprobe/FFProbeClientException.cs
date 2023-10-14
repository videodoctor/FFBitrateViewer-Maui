using System;

namespace FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;

[Serializable]
public class FFProbeClientException : ApplicationException
{
    public FFProbeClientException() { }
    public FFProbeClientException(string message) : base(message) { }
    public FFProbeClientException(string message, Exception inner) : base(message, inner) { }
    protected FFProbeClientException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
