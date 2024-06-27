using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;
using System;
using System.Collections.Generic;


namespace FFBitrateViewer.ApplicationAvalonia.Models.Media;

public record AudioStream : BaseStream
{
    public BitRate? BitRate { get; set; }
    public string? Channels { get; set; }
    public string? Encoder { get; set; }
    public UInt? Frequency { get; set; }

    public static new AudioStream Build(FFProbeStream info)
    {
        ArgumentNullException.ThrowIfNull(info);

        var audioStream = new AudioStream();

        PopulateBaseStream(ref info, ref audioStream);

        if (info.BitRate is not null)
        { audioStream.BitRate = new BitRate((int)info.BitRate); }

        if (info.ChannelLayout is not null)
        { audioStream.Channels = info.ChannelLayout; }

        if (info.SampleRate is not null)
        { audioStream.Frequency = new SampleRate((int)info.SampleRate); }

        return audioStream;
    }

    public override string ToString()
    {
        var result = new List<string>();
        if (Encoder is not null) result.Add(Encoder);
        if (Channels is not null) result.Add(Channels);
        if (BitRate is not null) result.Add(BitRate.ToString());
        if (Frequency is not null) result.Add(Frequency.ToString());
        return string.Join(", ", result);
    }

}