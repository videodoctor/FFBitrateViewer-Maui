using CommunityToolkit.Mvvm.ComponentModel;
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.ffprobe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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


}