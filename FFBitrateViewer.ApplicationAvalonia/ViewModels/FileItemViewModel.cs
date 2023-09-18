using CommunityToolkit.Mvvm.ComponentModel;
using FFBitrateViewer.ApplicationAvalonia.Extensions;
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.ffprobe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace FFBitrateViewer.ApplicationAvalonia.ViewModels
{
    public partial class FileItemViewModel : ViewModelBase
    {
        public static readonly Uri AboutBlankUri = new("about:blank");

        [ObservableProperty]
        private bool _isSelected;

        #region File Info
        [ObservableProperty]
        private Uri _path = AboutBlankUri;
        #endregion

        #region Media Info
        [ObservableProperty]
        private double _startTime;

        [ObservableProperty]
        private double? _duration;

        [ObservableProperty]
        private BitRate? _bitrate;

        [ObservableProperty]
        private string _firstVideoShortDesc = string.Empty;

        public List<VideoStream> VideoStreams { get; } = new();
        public List<AudioStream> AudioStreams { get; } = new();
        public List<SubtitleStream> SubtitleStreams { get; } = new();

        #endregion

        private readonly FileEntry _fileEntry;
        private readonly FFProbeJsonOutput _mediaInfo;

        public FileItemViewModel(FileEntry fileEntry, FFProbeJsonOutput mediaInfo)
        {
            ArgumentException.ThrowIfNullOrEmpty(nameof(fileEntry));
            ArgumentException.ThrowIfNullOrEmpty(nameof(mediaInfo));

            _fileEntry = fileEntry;
            _mediaInfo = mediaInfo;

            InitializeViewModel(ref _fileEntry, ref _mediaInfo);
        }

        private void InitializeViewModel(ref FileEntry fileEntry, ref FFProbeJsonOutput mediaInfo)
        {
            Path = _fileEntry.Path;

            StartTime = mediaInfo.Format?.StartTime ?? 0;
            Duration = mediaInfo.GetDuration();
            Bitrate = mediaInfo.Format?.BitRate == null ? Bitrate : new BitRate(mediaInfo.Format.BitRate.Value);

            IEnumerable<FFProbeStream> streams = mediaInfo.Streams ?? Enumerable.Empty<FFProbeStream>();

            foreach (var stream in streams)
            {
                switch (stream.CodecType?.ToUpper())
                {
                    case "VIDEO":
                        // Attached pics are also added as Video Streams with CodecName = mjpeg (could be png?)
                        if (stream.CodecName?.ToUpper() == "MJPEG")
                        { continue; }
                        VideoStreams.Add(VideoStream.Build(stream));
                        break;
                    case "AUDIO":
                        AudioStreams.Add(AudioStream.Build(stream));
                        break;
                    case "SUBTITLE":
                        SubtitleStreams.Add(SubtitleStream.Build(stream));
                        break;
                }
            }

            FirstVideoShortDesc = VideoStreams.FirstOrDefault()?.ToString(VideoStreamToStringMode.SHORT) ?? string.Empty;
        }
    }

    [Flags]
    public enum Unit
    {
        Unknown = 0,
        // b/s
        BitsPerSecond = 1,
        // Hz
        Hertz = 2
    }

    public record UnitValue<T, V>(T Value, V Unit, V DefaultUnit);
    public record UInt(int Value, Unit Unit) : UnitValue<int, Unit>(Value, Unit, Unit.Unknown);
    public record UDouble(double Value, Unit Unit) : UnitValue<double, Unit>(Value, Unit, Unit.Unknown);
    public record BitRate(int Value) : UInt(Value, Unit.BitsPerSecond);
    public record SampleRate(int Value) : UInt(Value, Unit.Hertz);


    public record NDPair(string Value, int? Numerator, int? Denominator)
    {
        public static readonly NDPair Default = new(string.Empty, null, null);
        private static readonly Regex NDPairRegex = new(@"^(?<numerator>\d+)/(?<denominator>\d+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        public static NDPair Parse(string value)
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));

            var match = NDPairRegex.Match(value);

            if (!match.Success)
            { throw new FormatException($"Value '{value}' is not in the expected format."); }

            if (!match.Groups.TryGetValue("numerator", out var numeratorGroup))
            { throw new FormatException($"Value '{value}' is not in the expected format."); }

            if (!match.Groups.TryGetValue("denominator", out var denominatorGroup))
            { throw new FormatException($"Value '{value}' is not in the expected format."); }

            return new NDPair(value, int.Parse(numeratorGroup.Value), int.Parse(denominatorGroup.Value));
        }

        public double? ToDouble()
        {
            if (Numerator == null || Denominator == null)
            { return null; }
            return (double)Numerator / (double)Denominator;
        }

        public string ToString(bool isNumberOnly)
        {
            if (isNumberOnly)
            {
                return $"{ToDouble():F3}";
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(nameof(NDPair));
            stringBuilder.Append(" { ");
            if (PrintMembers(stringBuilder))
            {
                stringBuilder.Append(' ');
            }
            stringBuilder.Append('}');
            stringBuilder.AppendFormat("[ {0} / (1)= {2:F3} ]", Numerator, Denominator, ToDouble());
            return stringBuilder.ToString();
        }

    }
    public class BaseStream
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

    public enum VideoStreamFormatToStringMode
    {
        // FULL -- null
        CHROMA_SUBSAMPLING,
        COLOR_SPACE,
        COLOR_SPACE_FULL,
        COLOR_RANGE,
        FIELD_TYPE,
        FIELD_TYPE_NAME,
        PIXEL_FORMAT
    }

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

    public record PInt(int X, int Y)
    {
        public string ToString(char separator)
        {
            return string.Concat(X, separator, Y);
        }
    }

    public enum VideoStreamToStringMode
    {
        // FULL -- null
        SHORT
    }

    public class VideoStream : BaseStream
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
            BaseStream.PopulateBaseStream(ref info, ref videoStream);

            videoStream.Format = VideoStreamFormat.Build(info);
            videoStream.Profile = info.Profile; //TODO: Check if this refernce is released

            if (info.BitRate != null)
            { videoStream.BitRate = new BitRate(info.BitRate.Value); }

            if (info.Width != null && info.Height != null)
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

                    if (Resolution != null)
                    { sb.Append(Resolution.ToString('x')); }

                    if (FrameRateAvg?.Value != null)
                    { sb.Append("-" + FrameRateAvg.ToString(isNumberOnly: true)); }

                    sb.Append(Format?.ToString(VideoStreamFormatToStringMode.FIELD_TYPE));
                    result.Add(sb.ToString());

                    var format = Format?.ToString();
                    if (!string.IsNullOrEmpty(format))
                    { result.Add(format); }

                    if (BitRate != null)
                    { result.Add(BitRate.ToString()); }

                    break;

                case VideoStreamToStringMode.SHORT:

                    if (Resolution != null)
                    { sb.Append(Resolution.Y); }

                    if (FrameRateAvg?.Value != null)
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

}