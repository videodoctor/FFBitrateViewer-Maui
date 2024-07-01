﻿using CommunityToolkit.Mvvm.ComponentModel;
using FFBitrateViewer.ApplicationAvalonia.Models.Media;
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;


namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

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

    [ObservableProperty]
    private double _bitRateAverage = double.NaN;

    [ObservableProperty]
    private double _bitRateMaximum = double.NaN;

    public List<FFProbePacket> Frames { get; } = [];

    public List<VideoStream> VideoStreams { get; } = [];

    public List<AudioStream> AudioStreams { get; } = [];

    public List<SubtitleStream> SubtitleStreams { get; } = [];

    #endregion

    public IPlottable? Scatter { get; set; }

    public IFileEntry? FileEntry { get; init; }

    public FFProbeJsonOutput? MediaInfo { get; init; }

    public void Initialize()
    {
        Path = FileEntry?.Path ?? AboutBlankUri;

        StartTime = MediaInfo?.Format?.StartTime ?? 0;
        Duration = MediaInfo?.GetDuration() ?? 0;
        Bitrate = MediaInfo?.Format?.BitRate == null ? Bitrate : new BitRate(MediaInfo.Format.BitRate.Value);

        var streams = (MediaInfo?.Streams ?? Enumerable.Empty<FFProbeStream>()).ToArray();

        for (int streamIndex = 0; streamIndex < streams.Length; streamIndex++)
        {
            FFProbeStream? stream = streams[streamIndex];
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

    public double GetAverageBitRate(
        IList<FFProbePacket>? frames = null,
        double? adjustmentStartTime = null,
        int? magnitudeOrder = null
    )
    {
        frames ??= Frames;

        if (frames.Count == 0)
        { return double.NaN; }

        double adjustment = adjustmentStartTime ?? 0.0;
        double duration = frames[^1].PTSTime ?? 0 + frames[^1].DurationTime ?? 0 - adjustment;
        double bitrateAverage = frames.Sum(f => f.Size ?? 0) / duration * 8.0;


        bitrateAverage = double.Round(bitrateAverage / (magnitudeOrder ?? 1));

        return bitrateAverage;
    }

    public double GetBitRateMaximum(
        IList<FFProbePacket>? frames = null,
        double intervalDuration = 1,
        double intervalStartTime = 0,
        int? magnitudeOrder = null,
        bool hasToUpdateBitratesInAllFrames = false
    )
    {
        frames ??= Frames;

        // forcing the update of all bit rates by setting 1st to double.NaN
        if (hasToUpdateBitratesInAllFrames is true && frames.Count > 0)
        { frames[0].BitRate = double.NaN; }
        TryUpdateBitratesInAllFrames(frames, intervalDuration, intervalStartTime);


        var bitrates = frames.Select(f => f.BitRate);
        if (bitrates is null || !bitrates.Any())
        { return double.NaN; }

        double bitRateMaximum = bitrates.Max() / (magnitudeOrder ?? 1);

        return bitRateMaximum;
    }

    private void TryUpdateBitratesInAllFrames(
        IList<FFProbePacket> frames,
        double intervalDuration = 1,
        double intervalStartTime = 0
    )
    {
        var isAnyBitrateNotANumber = frames.Any(f => double.IsNaN(f.BitRate));
        if (isAnyBitrateNotANumber is false)
        { return; }

        int bitrate;
        double intervalSize = 0;
        double nextIntervalSize = 0;

        var indexes = new List<int>();

        for (int frameNumber = 0; frameNumber < frames.Count; ++frameNumber)
        {
            frames[frameNumber].BitRate = 0;
            var frame = frames[frameNumber];
            double duration = frame.DurationTime ?? 0;
            double size = frame.Size ?? 0;
            double startTime = frame.PTSTime ?? 0;

            // The packet is longer than interval
            if (duration > intervalDuration)
            {
                // => 2
                int fullIntervalsCount = (int)double.Truncate(duration / intervalDuration);
                // => x * 10 / 25
                int sizePerFullInterval = (int)double.Round(size * intervalDuration / duration);
                // => 5
                duration %= intervalDuration;
                size -= fullIntervalsCount * sizePerFullInterval;
                startTime += fullIntervalsCount * intervalDuration;

                // todo@ Show it somehow
                //if (sizePerFullInterval > max) max = sizePerFullInterval; 
            }

            if (startTime > (intervalStartTime + intervalDuration))
            {
                // A new interval is just started
                // Updating BitRate for frames in prev. interval
                bitrate = (int)double.Round(intervalSize / intervalDuration * 8);
                foreach (var index in indexes)
                { frames[index].BitRate = bitrate; }
                //{ frames[index].BitRate = bitrate; }
                indexes.Clear();

                //if (intervalSize > max) max = intervalSize;
                intervalStartTime += intervalDuration;
                intervalSize = nextIntervalSize;
            }

            if ((startTime + duration) < (intervalStartTime + intervalDuration))
            {
                // The packet is ended in the current interval, so its size is fully accounted to current interval
                intervalSize += size;
                nextIntervalSize = 0;
            }
            else
            {
                // The packet is ended in the next interval, so only part of the packet's size is accounted to size of the current interval
                int sizeForPart = (int)double.Round(size * ((intervalStartTime + intervalDuration) - startTime) / intervalDuration);
                intervalSize += sizeForPart;
                nextIntervalSize = size - sizeForPart;
            }
            indexes.Add(frameNumber);
        }

        //if (intervalSize > max) max = intervalSize; // last part
        bitrate = (int)double.Round(intervalSize / intervalDuration * 8);
        foreach (var index in indexes)
        { frames[index].BitRate = bitrate; }
        //{ frames[index].BitRate = bitrate; }
        indexes.Clear();

    }

}