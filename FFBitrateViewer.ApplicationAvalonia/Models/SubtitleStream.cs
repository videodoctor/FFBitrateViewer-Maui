using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;


namespace FFBitrateViewer.ApplicationAvalonia.Models;

public record SubtitleStream : BaseStream
{
    // todo@ override ToString
    public static new SubtitleStream Build(FFProbeStream info)
    {
        var subtitleStream = new SubtitleStream();
        BaseStream.PopulateBaseStream(ref info, ref subtitleStream);
        return subtitleStream;
    }
}