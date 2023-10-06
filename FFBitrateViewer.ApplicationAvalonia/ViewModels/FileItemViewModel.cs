using CommunityToolkit.Mvvm.ComponentModel;
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.ffprobe;
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

    public List<FFProbePacket> Frames { get; } = new();

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
        Path = fileEntry.Path;

        StartTime = mediaInfo.Format?.StartTime ?? 0;
        Duration = mediaInfo.GetDuration();
        Bitrate = mediaInfo.Format?.BitRate == null ? Bitrate : new BitRate(mediaInfo.Format.BitRate.Value);

        var streams = (mediaInfo.Streams ?? Enumerable.Empty<FFProbeStream>()).ToArray();

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

    public void RefreshBitRateAverage(bool isAdjustmentStartTime = false, double startTime = 0.0)
    {
        var bitRateAverage = GetAverageBitRate(
            Frames,
            isAdjustmentStartTime,
            startTime
        );

        if (double.IsNaN(bitRateAverage))
        { return; }

        BitRateAverage = double.Round(bitRateAverage / 1000);
    }

    public double GetAverageBitRate(
        IList<FFProbePacket> frames,
        bool isAdjustmentStartTime = false,
        double startTime = 0.0)
    {
        if (frames.Count == 0)
        { return double.NaN; }

        double adjustment = isAdjustmentStartTime ? startTime : 0;
        var duration = GetDurationFromFrames(frames, adjustment);

        var bitrateAverage = frames.Sum(f => f.Size) / duration * 8;
        return bitrateAverage ?? double.NaN;
    }

    private double? GetDurationFromFrames(IList<FFProbePacket> frames, double adjustment = 0)
    {
        if (frames.Count == 0)
        { return null; }
        return frames[^1].PTSTime ?? 0 + frames[^1].DurationTime ?? 0 - adjustment;
    }

    public double? GetDurationFromStream(IList<VideoStream> videoStreams)
    {
        if (videoStreams.Count == 0 || videoStreams[0] == null)
        { return null; }
        var video0 = videoStreams[0];
        return (video0.Duration > 0) ? (video0.Duration - (video0.StartTime ?? 0)) : null;
    }

    public double? GetDurationFromFileInfo(FFProbeJsonOutput mediaInfo)
    {
        if (mediaInfo == null)
        { return null; }

        var mediaInfoDuration = GetDurationFromStreams(mediaInfo);
        var startTime = mediaInfo.Format?.StartTime ?? 0;

        return (mediaInfoDuration > 0) ? (mediaInfoDuration - startTime) : null;
    }

    public double? GetDurationFromStreams(FFProbeJsonOutput ffProbeOutput)
    {
        if (ffProbeOutput == null)
        { return null; }

        if (ffProbeOutput.Format?.Duration != null)
        { return ffProbeOutput.Format.Duration.Value; }

        if (ffProbeOutput.Streams != null)
        {
            // todo@ should start is taken into consideration?
            var durationQuery = from stream in ffProbeOutput.Streams
                                where stream.StartTime != null && stream.Duration != null
                                let endTime = stream.StartTime + stream.Duration
                                select endTime;

            return durationQuery.FirstOrDefault();
        }

        return null;
    }

    public double? GetDuration()
    {
        return GetDurationFromFrames(Frames) ?? GetDurationFromStream(VideoStreams) ?? GetDurationFromFileInfo(_mediaInfo);
    }

    public void RefreshBitRateMaximum(
        double intervalDuration = 1,
        double intervalStartTime = 0
    )
    {
        var bitRateMaximum = GetBitRateMaximum(intervalDuration, intervalStartTime);
        
        if (bitRateMaximum == null)
        { return; }

        BitRateMaximum = bitRateMaximum.Value / 1000;
    }

    public double? GetBitRateMaximum(
        double intervalDuration = 1,
        double intervalStartTime = 0
    )
    {
        var bitrates = GetBitRates(intervalDuration, intervalStartTime);
        
        if (bitrates == null || bitrates.Count == 0)
        { return null; }

        return bitrates.Max();
    }

    public ICollection<int> GetBitRates(
        double intervalDuration = 1,
        double intervalStartTime = 0
    )
    {
        if (Frames.Count == 0 || intervalDuration == 0)
        { return Array.Empty<int>(); }

        int bitrate;
        double intervalSize = 0;
        double nextIntervalSize = 0;

        var indexes = new List<int>();
        var bitrates = new List<int>();

        for (int frameNumber = 0; frameNumber < Frames.Count; ++frameNumber)
        {
            bitrates.Add(0);
            var frame = Frames[frameNumber];
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
                { bitrates[index] = bitrate; }
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
        { bitrates[index] = bitrate; }
        //{ frames[index].BitRate = bitrate; }
        indexes.Clear();

        return bitrates;
    }

}