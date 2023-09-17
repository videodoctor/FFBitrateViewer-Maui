using CommunityToolkit.Mvvm.ComponentModel;
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.ffprobe;
using System;
using System.Collections;
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
        #endregion

        private readonly FileEntry _fileEntry;
        private readonly FFProbeJsonOutput _mediaInfo;

        public FileItemViewModel(FileEntry fileEntry, FFProbeJsonOutput mediaInfo)
        {
            ArgumentException.ThrowIfNullOrEmpty(nameof(fileEntry));
            ArgumentException.ThrowIfNullOrEmpty(nameof(mediaInfo));

            _fileEntry = fileEntry;
            _mediaInfo = mediaInfo;

            InitializeViewModel(ref fileEntry, ref mediaInfo);
        }

        private void InitializeViewModel(ref FileEntry fileEntry, ref FFProbeJsonOutput mediaInfo)
        {
            Path = _fileEntry.Path;

            StartTime = _mediaInfo.Format?.StartTime ?? 0;
            Duration = _mediaInfo.GetDuration();
            Bitrate = _mediaInfo.Format?.BitRate == null ? Bitrate : new BitRate(_mediaInfo.Format.BitRate.Value);

            IEnumerable<FFProbeStream> streams =  _mediaInfo.Streams ?? Enumerable.Empty<FFProbeStream>();

            foreach (var stream in streams)
            {

            }

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


    public record NDPair (string Value, int? Numerator, int? Denominator)
    {
        public static readonly NDPair Default = new(string.Empty, null, null);
        private static readonly Regex NDPairRegex  = new(@"^(?<numerator>\d+)/(?<denominator>\d+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
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

        public double? ToDouble(){
            if (Numerator == null || Denominator == null)
            { return null; }
            return (double)Numerator / (double)Denominator;
        }

        public override string ToString()
        {
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

    // public abstract record BaseStream
    // (
    //     string CodecName,
    //     string CodecTag,
    //     string CodecTagString,
    //     double? Duration,
    //     long? DurationTS,
    //     NDPair? FrameRateAvg,
    //     NDPair? FrameRateR,
    //     string Id,
    //     int? Index,
    //     long? StartPTS,
    //     double? StartTime,
    //     NDPair? TimeBase
    // );

    // public record VideoStreamFormat
    // (
    //     bool? Progressive,
    //     string PixelFormat,
    //     string ColorSpace,
    //     string ChromaSubsampling,
    //     string ColorRange
    // ){
    //     public static VideoStreamFormat Build(string colorRange,string pixFmt,string fieldOrder){



    //         return new VideoStreamFormat
    //         (
    //             Progressive: ,
    //             PixelFormat: pixFmt,
    //             ColorSpace: ,
    //             ChromaSubsampling: ,
    //             ColorRange: colorRange?.ToLower() switch
    //             {
    //                 "TV" => colorRange,
    //                 "PC" => colorRange,
    //                 _ => null
    //             }
    //         );
    //     }
    // }

    // public record VideoStream
    // (
    //     string CodecName,
    //     string CodecTag,
    //     string CodecTagString,
    //     double? Duration,
    //     long? DurationTS,
    //     NDPair? FrameRateAvg,
    //     NDPair? FrameRateR,
    //     string Id,
    //     int? Index,
    //     long? StartPTS,
    //     double? StartTime,
    //     NDPair? TimeBase,
    //     // Video Properties
    //     BitRate? BitRate,
    //     bool IsBitrateCalculated,
    //     string Encoder,

    // ) : BaseStream(
    //     CodecName,
    //     CodecTag,
    //     CodecTagString,
    //     Duration,
    //     DurationTS,
    //     FrameRateAvg,
    //     FrameRateR,
    //     Id,
    //     Index,
    //     StartPTS,
    //     StartTime,
    //     TimeBase
    // );
}