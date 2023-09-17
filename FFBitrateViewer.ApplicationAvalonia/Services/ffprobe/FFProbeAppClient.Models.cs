using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FFBitrateViewer.ApplicationAvalonia.Services.ffprobe
{

#nullable disable

    #region Get MediaInfo Models

    public class FFProbeJsonOutput
    {
        /// <summary>
        /// Information about the container
        /// </summary>
        [JsonPropertyName("format")]
        public FFProbeFormat Format { get; set; }

        ///// <summary>
        ///// Information about frames
        ///// </summary>
        //[JsonPropertyName("frames")]
        //public List<FFProbeFrame> Frames { get; set; }

        /// <summary>
        /// Information about packets
        /// </summary>
        [JsonPropertyName("packets")]
        public List<FFProbePacket> Packets { get; set; }

        /// <summary>
        /// Information about streams
        /// </summary>
        [JsonPropertyName("streams")]
        public List<FFProbeStream> Streams { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }

    public class FFProbeFormat
    {
        [JsonPropertyName("bit_rate")]
        public int? BitRate { get; set; }

        /// <summary>
        /// Approximate duration in seconds (stream can start *after* the 00:00:00 timecode).
        /// </summary>
        [JsonPropertyName("duration")]
        public double? Duration { get; set; }

        [JsonPropertyName("filename")]
        public string FileName { get; set; }

        [JsonPropertyName("format_long_name")]
        public string FormatLongName { get; set; }

        [JsonPropertyName("format_name")]
        public string FormatName { get; set; }

        [JsonPropertyName("probe_score")]
        public int? ProbeScore { get; set; }

        [JsonPropertyName("nb_programs")]
        public int? ProgramsCount { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }

        [JsonPropertyName("start_time")]
        public double? StartTime { get; set; }

        [JsonPropertyName("nb_streams")]
        public int? StreamsCount { get; set; }

        /// <summary>
        /// Container and format tags/metadata, not stream-specific tags.
        /// </summary>
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }

    public class FFProbePacket
    {
        [JsonPropertyName("codec_type")]
        public string CodecType { get; set; }

        /// <summary>
        /// decoding time stamp -- how packets stored in stream
        /// </summary>
        [JsonPropertyName("dts")]
        public int? DTS { get; set; }

        [JsonPropertyName("dts_time")]
        public double? DTSTime { get; set; }

        [JsonPropertyName("duration")]
        public int? Duration { get; set; }

        [JsonPropertyName("duration_time")]
        public double? DurationTime { get; set; }

        [JsonPropertyName("flags")]
        public string Flags { get; set; }

        /// <summary>
        /// presentation time stamp -- how packets should be displayed
        /// </summary>
        [JsonPropertyName("pts")]
        public int? PTS { get; set; }

        [JsonPropertyName("pts_time")]
        public double? PTSTime { get; set; }

        [JsonPropertyName("size")]
        public int? Size { get; set; }

        [JsonPropertyName("stream_index")]
        public int? StreamIndex { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }

    public class FFProbeStream
    {
        [JsonPropertyName("bit_rate")]
        public int? BitRate { get; set; }

        [JsonPropertyName("bits_per_sample")]
        public int? BitsPerSample { get; set; }

        [JsonPropertyName("bits_per_raw_sample")]
        public string BitsPerSampleRaw { get; set; }

        [JsonPropertyName("channel_layout")]
        public string ChannelLayout { get; set; }

        [JsonPropertyName("channels")]
        public int? Channels { get; set; }

        [JsonPropertyName("chroma_location")]
        public string ChromaLocation { get; set; }

        [JsonPropertyName("codec_name")]
        public string CodecName { get; set; }

        /// <summary>
        /// H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10
        /// </summary>
        [JsonPropertyName("codec_long_name")]
        public string CodecLongName { get; set; }

        [JsonPropertyName("codec_type")]
        public string CodecType { get; set; }

        [JsonPropertyName("codec_tag")]
        public string CodecTag { get; set; }

        /// <summary>
        /// Video codec's FourCC or audio codec's TwoCC
        /// </summary>
        [JsonPropertyName("codec_tag_string")]
        public string CodecTagString { get; set; }

        [JsonPropertyName("coded_height")]
        public int? CodedHeight { get; set; }

        [JsonPropertyName("coded_width")]
        public int? CodedWidth { get; set; }

        [JsonPropertyName("color_range")]
        public string ColorRange { get; set; }

        [JsonPropertyName("display_aspect_ratio")]
        public string DAR { get; set; }

        [JsonPropertyName("duration")]
        public double? Duration { get; set; }

        /// <summary>
        /// Duration expressed in integer time-base units (https://video.stackexchange.com/questions/27546/difference-between-duration-ts-and-duration-in-ffprobe-output
        /// </summary>
        [JsonPropertyName("duration_ts")]
        public long? DurationTS { get; set; }

        [JsonPropertyName("field_order")]
        public string FieldOrder { get; set; }

        [JsonPropertyName("nb_frames")]
        public int? FramesCount { get; set; }

        [JsonPropertyName("r_frame_rate")]
        public string FrameRateR { get; set; }

        [JsonPropertyName("avg_frame_rate")]
        public string FrameRateAvg { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("is_avc")]
        [JsonConverter(typeof(TruthyOrFalsyConverter))]
        public bool? IsAVC { get; set; }

        //  todo@ type bool?
        [JsonPropertyName("has_b_frames")]
        public int? IsHasBFrames { get; set; }

        [JsonPropertyName("level")]
        public int? Level { get; set; }

        [JsonPropertyName("nb_packets")]
        public int? PacketsCount { get; set; }

        [JsonPropertyName("pix_fmt")]
        public string PixFmt { get; set; }

        [JsonPropertyName("profile")]
        public string Profile { get; set; }

        [JsonPropertyName("refs")]
        public int? Refs { get; set; }

        [JsonPropertyName("sample_fmt")]
        public string SampleFormat { get; set; }

        [JsonPropertyName("sample_rate")]
        public int? SampleRate { get; set; }

        [JsonPropertyName("sample_aspect_ratio")]
        public string SAR { get; set; }

        [JsonPropertyName("start_pts")]
        public long? StartPTS { get; set; }

        [JsonPropertyName("start_time")]
        public double? StartTime { get; set; }

        /// <summary>
        /// Stream-specific tags/metadata. See <see cref="KnownFFProbeVideoStreamTags"/>.
        /// </summary>
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; }

        /// <summary>
        /// Values like &quot;1/600&quot;. See https://stackoverflow.com/questions/43333542/what-is-video-timescale-timebase-or-timestamp-in-ffmpeg 
        /// </summary>
        [JsonPropertyName("time_base")]
        public string TimeBase { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
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
    #endregion
#nullable enable
}
