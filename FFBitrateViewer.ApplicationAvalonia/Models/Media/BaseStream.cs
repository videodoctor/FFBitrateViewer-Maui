using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;
using System;


namespace FFBitrateViewer.ApplicationAvalonia.Models.Media;

public record BaseStream
{
    public string? CodecName { get; set; }
    public string? CodecTag { get; set; }
    public string? CodecTagString { get; set; }
    public double? Duration { get; set; }
    public long? DurationTS { get; set; }
    public NDPair? FrameRateAvg { get; set; }
    public NDPair? FrameRateR { get; set; }
    public string? Id { get; set; }
    public int? Index { get; set; }
    public long? StartPTS { get; set; }
    public double? StartTime { get; set; }
    public NDPair? TimeBase { get; set; }

    public static BaseStream Build(FFProbeStream info)
    {

        ArgumentNullException.ThrowIfNull(info);

        var baseStream = new BaseStream();
        PopulateBaseStream(ref info, ref baseStream);
        return baseStream;

    }

    internal static void PopulateBaseStream<TBaseStream>(ref FFProbeStream info, ref TBaseStream baseStream) where TBaseStream : BaseStream
    {
        //Info = info;
        baseStream.CodecName = info.CodecName;
        baseStream.CodecTag = info.CodecTag;
        baseStream.CodecTagString = info.CodecTagString;
        baseStream.Duration = info.Duration;
        baseStream.DurationTS = info.DurationTS;
        baseStream.FrameRateAvg = NDPair.Parse(info.FrameRateAvg);
        baseStream.FrameRateR = NDPair.Parse(info.FrameRateR);
        baseStream.Id = info.Id;
        baseStream.Index = info.Index;
        baseStream.StartPTS = info.StartPTS;
        baseStream.StartTime = info.StartTime;
        baseStream.TimeBase = NDPair.Parse(info.TimeBase);
    }
}