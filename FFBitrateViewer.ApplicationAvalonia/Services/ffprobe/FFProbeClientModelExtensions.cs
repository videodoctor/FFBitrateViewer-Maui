using System.Linq;

namespace FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;

public static class FFProbeClientModelExtensions
{
    public static double? GetDuration(this FFProbeJsonOutput ffProbeOutput)
    {
        if (ffProbeOutput is null)
        { return null; }

        if (ffProbeOutput.Format?.Duration is not null)
        { return ffProbeOutput.Format.Duration; }

        if (ffProbeOutput.Streams is null)
        { return null; }

        double? result = (
            from stream in ffProbeOutput.Streams
            let duration = stream.Duration
            let start = stream.StartTime ?? 0
            where duration is not null
            select duration + start
            )
            .FirstOrDefault()
            ;
        return result;

    }
}