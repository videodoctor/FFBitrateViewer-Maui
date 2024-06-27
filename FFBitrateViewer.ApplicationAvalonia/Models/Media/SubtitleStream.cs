using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;


namespace FFBitrateViewer.ApplicationAvalonia.Models.Media;

public record SubtitleStream : BaseStream
{
    // todo@ override ToString
    public static new SubtitleStream Build(FFProbeStream info)
    {
        var subtitleStream = new SubtitleStream();
        PopulateBaseStream(ref info, ref subtitleStream);
        return subtitleStream;
    }
}