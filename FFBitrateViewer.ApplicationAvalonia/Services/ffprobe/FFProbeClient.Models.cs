using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FFBitrateViewer.ApplicationAvalonia.Services.FFProbe
{

#nullable disable

    public record FFProbeJsonOutput
    (
        /// <summary>
        /// Information about the container
        /// </summary>
        [property: JsonPropertyName("format")]
        FFProbeFormat Format,

        ///// <summary>
        ///// Information about frames
        ///// </summary>
        //[property: JsonPropertyName("frames")]
        //List<FFProbeFrame> Frames,

        /// <summary>
        /// Information about packets
        /// </summary>
        [property: JsonPropertyName("packets")]
        List<FFProbePacket> Packets,

        /// <summary>
        /// Information about streams
        /// </summary>
        [property: JsonPropertyName("streams")]
        List<FFProbeStream> Streams

    // [property: JsonExtensionData]
    // Dictionary<string, JsonElement> ExtensionData
    );

    public record FFProbeFormat
   (
       [property: JsonPropertyName("bit_rate")]
        int? BitRate,

       /// <summary>
       /// Approximate duration in seconds (stream can start *after* the 00:00:00 timecode).
       /// </summary>
       [property: JsonPropertyName("duration")]
        double? Duration,

       [property: JsonPropertyName("filename")]
        string FileName,

       [property: JsonPropertyName("format_long_name")]
        string FormatLongName,

       [property: JsonPropertyName("format_name")]
        string FormatName,

       [property: JsonPropertyName("probe_score")]
        int? ProbeScore,

       [property: JsonPropertyName("nb_programs")]
        int? ProgramsCount,

       [property: JsonPropertyName("size")]
        long? Size,

       [property: JsonPropertyName("start_time")]
        double? StartTime,

       [property: JsonPropertyName("nb_streams")]
        int? StreamsCount,

       /// <summary>
       /// Container and format tags/metadata, not stream-specific tags.
       /// </summary>
       [property: JsonPropertyName("tags")]
        Dictionary<string, string> Tags

   // [property: JsonExtensionData]
   // Dictionary<string, JsonElement> ExtensionData
   );

    public record FFProbePacket
    (
        [property: JsonPropertyName("codec_type")]
        string CodecType,

        /// <summary>
        /// decoding time stamp -- how packets stored in stream
        /// </summary>
        [property: JsonPropertyName("dts")]
        int? DTS,

        [property: JsonPropertyName("dts_time")]
        double? DTSTime,

        [property: JsonPropertyName("duration")]
        double? Duration,

        [property: JsonPropertyName("duration_time")]
        double? DurationTime,

        [property: JsonPropertyName("flags")]
        string Flags,

        /// <summary>
        /// presentation time stamp -- how packets should be displayed
        /// </summary>
        [property: JsonPropertyName("pts")]
        int? PTS,

        [property: JsonPropertyName("pts_time")]
        double? PTSTime,

        [property: JsonPropertyName("size")]
        long? Size,

        [property: JsonPropertyName("stream_index")]
        int? StreamIndex

    // [property: JsonExtensionData]
    // Dictionary<string, JsonElement> ExtensionData
    )
    {

        [JsonIgnore]
        public double BitRate { get; set; } = Double.NaN;

    }

    public record FFProbeStream
    (
        [property: JsonPropertyName("bit_rate")]
        int? BitRate,

        [property: JsonPropertyName("bits_per_sample")]
        int? BitsPerSample,

        [property: JsonPropertyName("bits_per_raw_sample")]
        string BitsPerSampleRaw,

        [property: JsonPropertyName("channel_layout")]
        string ChannelLayout,

        [property: JsonPropertyName("channels")]
        int? Channels,

        [property: JsonPropertyName("chroma_location")]
        string ChromaLocation,

        [property: JsonPropertyName("codec_name")]
        string CodecName,

        /// <summary>
        /// H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10
        /// </summary>
        [property: JsonPropertyName("codec_long_name")]
        string CodecLongName,

        [property: JsonPropertyName("codec_type")]
        string CodecType,

        [property: JsonPropertyName("codec_tag")]
        string CodecTag,

        /// <summary>
        /// Video codec's FourCC or audio codec's TwoCC
        /// </summary>
        [property: JsonPropertyName("codec_tag_string")]
        string CodecTagString,

        [property: JsonPropertyName("coded_height")]
        int? CodedHeight,

        [property: JsonPropertyName("coded_width")]
        int? CodedWidth,

        [property: JsonPropertyName("color_range")]
        string ColorRange,

        [property: JsonPropertyName("display_aspect_ratio")]
        string DAR,

        [property: JsonPropertyName("duration")]
        double? Duration,

        /// <summary>
        /// Duration expressed in integer time-base units (https://video.stackexchange.com/questions/27546/difference-between-duration-ts-and-duration-in-ffprobe-output
        /// </summary>
        [property: JsonPropertyName("duration_ts")]
        long? DurationTS,

        [property: JsonPropertyName("field_order")]
        string FieldOrder,

        [property: JsonPropertyName("nb_frames")]
        int? FramesCount,

        [property: JsonPropertyName("r_frame_rate")]
        string FrameRateR,

        [property: JsonPropertyName("avg_frame_rate")]
        string FrameRateAvg,

        [property: JsonPropertyName("height")]
        int? Height,

        [property: JsonPropertyName("id")]
        string Id,

        [property: JsonPropertyName("index")]
        int Index,

        [property: JsonPropertyName("is_avc")]
        [property: JsonConverter(typeof(TruthyOrFalsyConverter))]
        bool? IsAVC,

        //  todo@ type bool?
        [property: JsonPropertyName("has_b_frames")]
        int? IsHasBFrames,

        [property: JsonPropertyName("level")]
        int? Level,

        [property: JsonPropertyName("nb_packets")]
        int? PacketsCount,

        [property: JsonPropertyName("pix_fmt")]
        string PixFmt,

        [property: JsonPropertyName("profile")]
        string Profile,

        [property: JsonPropertyName("refs")]
        int? Refs,

        [property: JsonPropertyName("sample_fmt")]
        string SampleFormat,

        [property: JsonPropertyName("sample_rate")]
        int? SampleRate,

        [property: JsonPropertyName("sample_aspect_ratio")]
        string SAR,

        [property: JsonPropertyName("start_pts")]
        long? StartPTS,

        [property: JsonPropertyName("start_time")]
        double? StartTime,

        /// <summary>
        /// Stream-specific tags/metadata. See <see cref="KnownFFProbeVideoStreamTags"/>.
        /// </summary>
        [property: JsonPropertyName("tags")]
        Dictionary<string, string> Tags,

        /// <summary>
        /// Values like &quot;1/600&quot;. See https://stackoverflow.com/questions/43333542/what-is-video-timescale-timebase-or-timestamp-in-ffmpeg
        /// </summary>
        [property: JsonPropertyName("time_base")]
        string TimeBase,

        [property: JsonPropertyName("width")]
        int? Width

    // [property: JsonExtensionData]
    // Dictionary<string, JsonElement> ExtensionData
    );

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

    /// <summary>
    /// Converts input to a True or False applying Javascript truthy or falsy value.
    /// <para>
    /// In JavaScript, a truthy value is a value that is considered true when encountered in a Boolean context.
    /// All values are truthy unless they are defined as falsy. That is, all values are truthy except false, 0, -0, 0n, "", null, undefined, and NaN.
    /// </para>
    /// </summary>
    ///
    internal class TruthyOrFalsyConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.False
                || reader.TokenType == JsonTokenType.Null)
            {
                _ = reader.GetBoolean();
                return false;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var intValue))
                {
                    return intValue != 0 && intValue != -0;
                }
                if (reader.TryGetDouble(out var doubleValue))
                {
                    return doubleValue != 0.0 && doubleValue != -0.0;
                }
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                return !string.IsNullOrEmpty(stringValue);
            }

            return true;
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

#nullable enable
}