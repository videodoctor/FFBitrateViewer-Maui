using System.Linq;

namespace FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;

public static class FFProbeExtensions
{
    public static double? GetDuration(this FFProbeJsonOutput ffProbeOutput)
    {
        if (ffProbeOutput == null)
        { return null; }

        if (ffProbeOutput.Format?.Duration != null)
        { return ffProbeOutput.Format.Duration; }

        if (ffProbeOutput.Streams == null)
        { return null; }

        double? result = (
            from stream in ffProbeOutput.Streams
            let duration = stream.Duration
            let start = stream.StartTime ?? 0
            where duration != null
            select duration + start
            )
            .FirstOrDefault()
            ;
        return result;

    }
}
