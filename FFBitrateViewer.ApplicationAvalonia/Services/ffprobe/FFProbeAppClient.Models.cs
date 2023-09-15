using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FFBitrateViewer.ApplicationAvalonia.Services.ffprobe
{
    // Models were generated automatically from https://jsonformatter.org/json-to-csharp

#nullable disable
    #region MediaInfo Models
    public partial class MediaInfo
    {
        [JsonPropertyName("streams")]
        public Stream[] Streams { get; set; }

        [JsonPropertyName("format")]
        public Format Format { get; set; }
    }

    public partial class Format
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("nb_streams")]
        public long? NbStreams { get; set; }

        [JsonPropertyName("nb_programs")]
        public long? NbPrograms { get; set; }

        [JsonPropertyName("format_name")]
        public string FormatName { get; set; }

        [JsonPropertyName("format_long_name")]
        public string FormatLongName { get; set; }

        [JsonPropertyName("start_time")]
        public string StartTime { get; set; }

        [JsonPropertyName("duration")]
        public string Duration { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }

        [JsonPropertyName("bit_rate")]
        public long? BitRate { get; set; }

        [JsonPropertyName("probe_score")]
        public long? ProbeScore { get; set; }

        [JsonPropertyName("tags")]
        public FormatTags Tags { get; set; }
    }

    public partial class FormatTags
    {
        [JsonPropertyName("creation_time")]
        public DateTimeOffset? CreationTime { get; set; }

        [JsonPropertyName("ENCODER")]
        public string Encoder { get; set; }
    }

    public partial class Stream
    {
        [JsonPropertyName("index")]
        public long? Index { get; set; }

        [JsonPropertyName("codec_name")]
        public string CodecName { get; set; }

        [JsonPropertyName("codec_long_name")]
        public string CodecLongName { get; set; }

        [JsonPropertyName("profile")]
        public string Profile { get; set; }

        [JsonPropertyName("codec_type")]
        public string CodecType { get; set; }

        [JsonPropertyName("codec_tag_string")]
        public string CodecTagString { get; set; }

        [JsonPropertyName("codec_tag")]
        public string CodecTag { get; set; }

        [JsonPropertyName("width")]
        public long? Width { get; set; }

        [JsonPropertyName("height")]
        public long? Height { get; set; }

        [JsonPropertyName("coded_width")]
        public long? CodedWidth { get; set; }

        [JsonPropertyName("coded_height")]
        public long? CodedHeight { get; set; }

        [JsonPropertyName("closed_captions")]
        public long? ClosedCaptions { get; set; }

        [JsonPropertyName("film_grain")]
        public long? FilmGrain { get; set; }

        [JsonPropertyName("has_b_frames")]
        public long? HasBFrames { get; set; }

        [JsonPropertyName("sample_aspect_ratio")]
        public string SampleAspectRatio { get; set; }

        [JsonPropertyName("display_aspect_ratio")]
        public string DisplayAspectRatio { get; set; }

        [JsonPropertyName("pix_fmt")]
        public string PixFmt { get; set; }

        [JsonPropertyName("level")]
        public long? Level { get; set; }

        [JsonPropertyName("color_range")]
        public string ColorRange { get; set; }

        [JsonPropertyName("color_space")]
        public string ColorSpace { get; set; }

        [JsonPropertyName("color_transfer")]
        public string ColorTransfer { get; set; }

        [JsonPropertyName("color_primaries")]
        public string ColorPrimaries { get; set; }

        [JsonPropertyName("chroma_location")]
        public string ChromaLocation { get; set; }

        [JsonPropertyName("refs")]
        public long? Refs { get; set; }

        [JsonPropertyName("r_frame_rate")]
        public string RFrameRate { get; set; }

        [JsonPropertyName("avg_frame_rate")]
        public string AvgFrameRate { get; set; }

        [JsonPropertyName("time_base")]
        public string TimeBase { get; set; }

        [JsonPropertyName("start_pts")]
        public long? StartPts { get; set; }

        [JsonPropertyName("start_time")]
        public string StartTime { get; set; }

        [JsonPropertyName("extradata_size")]
        public long? ExtradataSize { get; set; }

        [JsonPropertyName("disposition")]
        public System.Collections.Generic.Dictionary<string, long?> Disposition { get; set; }

        [JsonPropertyName("tags")]
        public StreamTags Tags { get; set; }

        [JsonPropertyName("sample_fmt")]
        public string SampleFmt { get; set; }

        //[JsonPropertyName("sample_rate")]
        ////[JsonConverter(typeof(ParseStringConverter))]
        //public long? SampleRate { get; set; }

        [JsonPropertyName("channels")]
        public long? Channels { get; set; }

        [JsonPropertyName("channel_layout")]
        public string ChannelLayout { get; set; }

        [JsonPropertyName("bits_per_sample")]
        public long? BitsPerSample { get; set; }

        [JsonPropertyName("initial_padding")]
        public long? InitialPadding { get; set; }

        [JsonPropertyName("duration_ts")]
        public long? DurationTs { get; set; }

        [JsonPropertyName("duration")]
        public string Duration { get; set; }
    }

    public partial class StreamTags
    {
        [JsonPropertyName("DURATION")]
        [JsonConverter(typeof(NullableTimeSpanConverter))]
        public TimeSpan? Duration { get; set; }
    }

    #endregion

    #region ProbePacket Model
    public partial class ProbePacket
    {
        [JsonPropertyName("pts_time")]
        public double? PtsTime { get; set; }

        [JsonPropertyName("duration_time")]
        public double? DurationTime { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }

        [JsonPropertyName("flags")]
        public string Flags { get; set; }

        [JsonPropertyName("dts_time")]
        public double? DtsTime { get; set; }
    }

    #endregion

    #region JsonConverters
    internal class NullableTimeSpanConverter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var textValue = reader.GetString();

            // NOTE: In hh:mm:ss.fffffffff , fffffffff  is in range [0000001 , 9999999],
            //       but we can receive 00:16:51.398000000.
            //       Hence we need to round the ticks up to 9 decimals
            const int TICKS_DECIMAL_PRESICION = 7;
            if (textValue.Split('.') is [var timeText, var ticksText])
            {
                // && double.TryParse($"0.{ticksText}", out var ticks)
                // && ticksText.Length > TICKS_DECIMAL_PRESICION
                // && ticksText[^1] == '0'

                // lets remove trailing zeros!
                if (ticksText[^1] == '0')
                { ticksText = ticksText.TrimEnd('0'); }

                // just in case we remove all numbers
                if (ticksText.Length == 0)
                { ticksText = "0"; }

                // still have too many numbers? lets round
                if (ticksText.Length > TICKS_DECIMAL_PRESICION
                    && double.TryParse($"0.{ticksText}", out var ticks))
                {
                    ticksText = $"{ticks:F7}"[2..^0]; //$"{ticks:F7}";
                }

                // recompose value
                textValue = $"{timeText}.{ticksText}";
            }

            // lets try to convert
            if (TimeSpan.TryParse(textValue, out var value))
            {
                return value;
            }

            return default;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            throw new NotSupportedException($"{nameof(NullableTimeSpanConverter)}.{Write} is not supported");
        }
    }
    #endregion
#nullable enable
}
