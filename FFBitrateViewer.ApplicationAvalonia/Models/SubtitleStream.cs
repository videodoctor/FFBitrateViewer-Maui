using FFBitrateViewer.ApplicationAvalonia.Services.ffprobe;


namespace FFBitrateViewer.ApplicationAvalonia.Models;

public class SubtitleStream : BaseStream
{
    // todo@ override ToString
    public static new SubtitleStream Build(FFProbeStream info)
    {
        var subtitleStream = new SubtitleStream();
        BaseStream.PopulateBaseStream(ref info, ref subtitleStream);
        return subtitleStream;
    }
}