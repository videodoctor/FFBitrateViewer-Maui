using System;

namespace FFBitrateViewer.ApplicationAvalonia.Services;

[Serializable]
public class OSProcessServiceException : ApplicationException
{
    public OSProcessServiceException() { }
    public OSProcessServiceException(string message) : base(message) { }
    public OSProcessServiceException(string message, Exception inner) : base(message, inner) { }
    protected OSProcessServiceException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}