using FFBitrateViewer.ApplicationAvalonia.Services.ffprobe;
using System;
using System.Collections.Generic;


namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public class AudioStream : BaseStream
{
    public BitRate? BitRate { get; set; }
    public string? Channels { get; set; }
    public string? Encoder { get; set; }
    public UInt? Frequency { get; set; }

    public static new AudioStream Build(FFProbeStream info)
    {
        ArgumentNullException.ThrowIfNull(info);

        var audioStream = new AudioStream();

        BaseStream.PopulateBaseStream(ref info, ref audioStream);

        if (info.BitRate != null)
        { audioStream.BitRate = new BitRate((int)info.BitRate); }

        if (info.ChannelLayout != null)
        { audioStream.Channels = info.ChannelLayout; }

        if (info.SampleRate != null)
        { audioStream.Frequency = new SampleRate((int)info.SampleRate); }

        return audioStream;
    }

    public override string? ToString()
    {
        var result = new List<string>();
        if (Encoder != null) result.Add(Encoder);
        if (Channels != null) result.Add(Channels);
        if (BitRate != null) result.Add(BitRate.ToString());
        if (Frequency != null) result.Add(Frequency.ToString());
        return string.Join(", ", result);
    }

}