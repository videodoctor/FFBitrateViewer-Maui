using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;
using System;
using System.Collections.Generic;
using System.Text;


namespace FFBitrateViewer.ApplicationAvalonia.Models.Media;

public record VideoStream : BaseStream
{
    public BitRate? BitRate { get; set; }
    public bool IsBitrateCalculated { get; set; }
    public string? Encoder { get; set; }
    public VideoStreamFormat? Format { get; set; }
    // public UDouble? FPS { get; set; }
    public string? Profile { get; set; }
    public PInt? Resolution { get; set; }
    // public UDouble? TBR { get; set; }

    public new static VideoStream Build(FFProbeStream info)
    {
        ArgumentNullException.ThrowIfNull(info);

        var videoStream = new VideoStream();
        PopulateBaseStream(ref info, ref videoStream);

        videoStream.Format = VideoStreamFormat.Build(info);
        videoStream.Profile = info.Profile; //TODO: Check if this refernce is released

        if (info.BitRate is not null)
        { videoStream.BitRate = new BitRate(info.BitRate.Value); }

        if (info.Width is not null && info.Height is not null)
        { videoStream.Resolution = new PInt(info.Width.Value, info.Height.Value); }

        return videoStream;
    }

    public string ToString(VideoStreamToStringMode? mode = null)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        switch (mode)
        {
            case null: // FULL

                if (Resolution is not null)
                { sb.Append(Resolution.ToString('x')); }

                if (FrameRateAvg?.Value is not null)
                { sb.Append("-" + FrameRateAvg.ToString(isNumberOnly: true)); }

                sb.Append(Format?.ToString(VideoStreamFormatToStringMode.FIELD_TYPE));
                result.Add(sb.ToString());

                var format = Format?.ToString();
                if (!string.IsNullOrEmpty(format))
                { result.Add(format); }

                if (BitRate is not null)
                { result.Add(BitRate.ToString()); }

                break;

            case VideoStreamToStringMode.SHORT:

                if (Resolution is not null)
                { sb.Append(Resolution.Y); }

                if (FrameRateAvg?.Value is not null)
                { sb.Append("-" + FrameRateAvg.ToString(isNumberOnly: true)); }

                sb.Append(Format?.ToString(VideoStreamFormatToStringMode.FIELD_TYPE));
                if (sb.Length > 0)
                { result.Add(sb.ToString()); }

                var colorSpaceText = Format?.ToString(VideoStreamFormatToStringMode.COLOR_SPACE_FULL) ?? string.Empty;
                if (!string.IsNullOrEmpty(colorSpaceText))
                { result.Add(colorSpaceText); }

                var colorRangeText = Format?.ToString(VideoStreamFormatToStringMode.COLOR_RANGE) ?? string.Empty;
                if (!string.IsNullOrEmpty(colorRangeText))
                { result.Add(colorRangeText); }

                break;

            default:
                break; // todo@ exception?
        }
        return string.Join(", ", result);
    }

}