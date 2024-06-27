using FFBitrateViewer.ApplicationAvalonia.Extensions;
using FFBitrateViewer.ApplicationAvalonia.Models.Media;
using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;
using System;
using System.Linq;
using System.Text.RegularExpressions;


namespace FFBitrateViewer.ApplicationAvalonia.Models;

public partial class VideoStreamFormat
{
    // private static readonly Regex PixelFormatRegex = new ("^(?<ColorSpaceSet>ABGR|ARGB|BGR|GBR|GRAY|RGB|UYVY|YA|YUV|YUVA|YUVJ|YUYV)(\\(?<ChromaSubsamplingSet>d{1,3})?(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex PixelFormatRegex = GeneratedPixelFormatRegex();

    // true -- progressive, false -- interlaced, null -- undetected
    public bool? Progressive { get; set; }
    // http://git.videolan.org/?p=ffmpeg.git;a=blob;f=libavutil/pixfmt.h;hb=HEAD
    public string? PixelFormat { get; set; }
    public string? ColorSpace { get; set; }
    public string? ChromaSubsampling { get; set; }
    public string? ColorRange { get; set; }

    // public static VideoStreamFormat Build(string colorRange,string pixelFormat,string fieldOrder)
    public static VideoStreamFormat Build(FFProbeStream info)
    {
        ArgumentNullException.ThrowIfNull(info);

        string? colorRange = info.ColorRange;
        string? pixelFormat = info.PixFmt;
        string? fieldOrder = info.FieldOrder;

        VideoStreamFormat videoStreamFormat = new()
        {
            // Color Range
            ColorRange = colorRange?.ToLower() switch
            {
                "tv" or "pc" => colorRange,
                _ => null
            },
        };

        // Pixel Format
        videoStreamFormat.PixelFormat = pixelFormat;
        var match = PixelFormatRegex.Match(pixelFormat ?? string.Empty);
        if (match.Success)
        {
            if (match.Groups.TryGetValue("ColorSpaceSet", out var colorSpaceSetGroup))
            {
                videoStreamFormat.ColorSpace = colorSpaceSetGroup.Value.ToUpper() switch
                {
                    "YUV" or "YUVJ" or "YUVY" or "YUYV" => "YUV",
                    "BGR" or "GBR" or "RGB" => "RGB",
                    "YUVA" => "YUVA",
                    "ABGR" or "ARGB" or "BGRA" or "RGBA" => "RGBA",
                    "GRAY" or "YA" or _ => null // todo@
                };
            }

            if (match.Groups.TryGetValue("ChromaSubsamplingSet", out var chromaSubsamplingSetGroup))
            {
                videoStreamFormat.ChromaSubsampling = chromaSubsamplingSetGroup.Value switch
                {
                    "420" or "422" or "440" or "444" => chromaSubsamplingSetGroup.Value,
                    _ => null // todo@
                };
            };

        }

        // Field Order
        if (fieldOrder?.Length > 0)
        {
            videoStreamFormat.Progressive = char.ToUpper(fieldOrder.First()) switch
            {
                'P' => true,
                'I' or 'B' or 'T' => false,
                _ => null
            };
        }

        return videoStreamFormat;
    }


    public string? ToString(VideoStreamFormatToStringMode? mode = null)
    {
        string? stringValue = mode switch
        {
            null => string.IsNullOrEmpty(ColorRange) ? PixelFormat : string.Concat(PixelFormat, " (", ColorRange, ")"),
            VideoStreamFormatToStringMode.CHROMA_SUBSAMPLING => ChromaSubsampling,
            VideoStreamFormatToStringMode.COLOR_RANGE => ColorRange?.ToUpper(),
            VideoStreamFormatToStringMode.COLOR_SPACE => ColorSpace,
            VideoStreamFormatToStringMode.COLOR_SPACE_FULL => string.Concat(ColorSpace, ChromaSubsampling),
            VideoStreamFormatToStringMode.FIELD_TYPE => Progressive.ToString(nullText: "?", trueText: "p", falseText: "i"),
            VideoStreamFormatToStringMode.FIELD_TYPE_NAME => Progressive.ToString(trueText: "Progressive", falseText: "Interlaced"),
            VideoStreamFormatToStringMode.PIXEL_FORMAT => PixelFormat,
            _ => null // todo@ exception
        };
        return stringValue;
    }

    [GeneratedRegex("^(?<ColorSpaceSet>ABGR|ARGB|BGR|GBR|GRAY|RGB|UYVY|YA|YUV|YUVA|YUVJ|YUYV)(?<ChromaSubsamplingSet>\\d{1,3})?(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, "en-US")]
    private static partial Regex GeneratedPixelFormatRegex();
}