using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.ffprobe;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels
{

    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _version = string.Empty;

        [ObservableProperty]

        private bool _isPlotterOn = false;

        [ObservableProperty]
        private bool _hasToAdjustFrameStartTime = false;

        [ObservableProperty]
        private FileItemViewModel? _selectedFile;

        public ObservableCollection<FileItemViewModel> Files { get; } = new();

        public PlotModel? PlotModelData { get; set; }
        public PlotController? PlotControllerData { get; set; }

        private PlotViewType _plotViewType = PlotViewType.FrameBased;

        private string AxisYUnitBuild => _plotViewType switch
        {
            PlotViewType.FrameBased => "kb",
            PlotViewType.SecondBased => "kb/s",
            PlotViewType.GOPBased => "kb/GOP",
            _ => throw new NotImplementedException($"Text for {nameof(AxisYUnitBuild)} equals to {_plotViewType} is not implemented.")
        };

        private string AxisYTitleBuild => _plotViewType switch
        {
            PlotViewType.FrameBased => "Frame size",
            PlotViewType.SecondBased => "Bit rate",
            PlotViewType.GOPBased => "Bit rate",
            _ => throw new NotImplementedException($"Text for {nameof(AxisYTitleBuild)} equals to {_plotViewType} is not implemented.")
        };

        private string TrackerFormatStringBuild => $@"{{0}}{Environment.NewLine}Time={{2:hh\:mm\:ss\.fff}}{Environment.NewLine}{{3}}={{4:0}} {AxisYUnitBuild}";

        private string AxisXStringFormatBuild(double? duration) =>
              (duration == null || duration.Value < 60) ? "m:ss"
            : (duration.Value < 60 * 60) ? "mm:ss"
            : "h:mm:ss"
            ;

        private readonly AppProcessService _appProcessService = new();
        private readonly FileDialogService _fileDialogService = new();
        private readonly FFProbeAppClient _ffprobeAppClient = new();


        [RelayCommand]
        private async Task OnLoaded(CancellationToken token)
        {

            if (PlotControllerData == null)
            { throw new ApplicationException($"Application failed connecting to {nameof(PlotControllerData)}"); }

            if (PlotModelData == null)
            { throw new ApplicationException($"Application failed connecting to {nameof(PlotModelData)}"); }

            var version = await _ffprobeAppClient.GetVersionAsync(token).ConfigureAwait(false);
            Version = $"{System.IO.Path.GetFileName(_ffprobeAppClient.FFProbeFilePath)} v{version}";

            // setting up Plot View
            PlotControllerData.UnbindMouseDown(OxyMouseButton.Left);
            PlotControllerData.BindMouseEnter(PlotCommands.HoverSnapTrack);

            // TODO: Move to XAML when possible
            #region Move to XAML when possible 
            var legend = PlotModelData.Legends.FirstOrDefault();
            if (legend != null)
            {
                legend.ShowInvisibleSeries = false;
                legend.LegendBackground = OxyColor.FromAColor(200, OxyColors.White);
            }
            for (int axisIndex = 0; axisIndex < PlotModelData.Axes.Count; axisIndex++)
            {
                OxyPlot.Axes.Axis? axis = PlotModelData.Axes[axisIndex];
                axis.MajorGridlineColor = OxyColor.FromArgb(40, 0, 0, 139);
                axis.MinorGridlineColor = OxyColor.FromArgb(20, 0, 0, 139);

                axis.AbsoluteMaximum = axis.Maximum = 1;
                if (axisIndex == 0)
                {
                    axis.AbsoluteMaximum = axis.Maximum = 10;
                    axis.StringFormat = AxisXStringFormatBuild(axis.Maximum);
                }
                if (axisIndex == 1)
                {
                    axis.Title = AxisYTitleBuild;
                    axis.Unit = AxisYUnitBuild;
                }

            }
            for (int serieIndex = 0; serieIndex < PlotModelData.Series.Count; serieIndex++)
            {
                OxyPlot.Series.Series? serie = PlotModelData.Series[serieIndex];
                serie.TrackerFormatString = TrackerFormatStringBuild;
            }
            #endregion

        }

        [RelayCommand]
        private void SetPlotViewType(PlotViewType plotViewType)
        {
            _plotViewType = plotViewType;
        }

        [RelayCommand]
        private async Task AddFiles(CancellationToken token)
        {
            // TODO: Check if we really need to use Task.Run
            await Task.Run(async () =>
            {
                var fileInfoEntries = await _fileDialogService.OpenAsync(IsSingleSelection: false).ConfigureAwait(false);
                for (int fileInfoIndex = 0; fileInfoIndex < fileInfoEntries.Count; fileInfoIndex++)
                {
                    var fileInfo = fileInfoEntries[fileInfoIndex];
                    var mediaInfo = await _ffprobeAppClient.GetMediaInfoAsync(fileInfo.Path.LocalPath).ConfigureAwait(false);
                    var fileItemViewModel = new FileItemViewModel(fileInfo, mediaInfo) { IsSelected = true };

                    // Add file to Data Grid
                    _appProcessService.ExecutionOnUIThread(() =>
                    {
                        Files.Add(fileItemViewModel);
                    });
                }
                SelectedFile = Files.LastOrDefault();
                PlotModelData!.InvalidatePlot(updateData: true);
            }, token).ConfigureAwait(false);
        }

        [RelayCommand(IncludeCancelCommand = true, FlowExceptionsToTaskScheduler = true)]
        private async Task ToggleOnOffPlotterPlotter(CancellationToken token)
        {

            double maxX = -1.0;
            double maxY = -1.0;

            PlotModelData!.Series.Clear();

            token.ThrowIfCancellationRequested();

            await Files
                .ToAsyncEnumerable()
                .Where(file => file.IsSelected)
                .ForEachAwaitAsync(async file =>
                {
                    token.ThrowIfCancellationRequested();

                    var fileName = Path.GetFileName(file.Path.LocalPath);
                    var serie = GetNewSerie(fileName);

                    // Request ffprobe information to create data points for serie
                    await _ffprobeAppClient
                        .GetProbePackets(file.Path.LocalPath, token: token)
                        .ForEachAsync(probePacket =>
                        {
                            file.Frames.Add(probePacket);
                            serie.Points.Add(GetDataPoint(probePacket, file, _plotViewType));
                        }, token).ConfigureAwait(false);

                    // Refresh BitRateAverage and BitRateMaximum
                    file.RefreshBitRateAverage();
                    file.RefreshBitRateMaximum();

                    // Computes axis x based on max duration
                    maxX = GetMaxXBasedOnDuration(file, maxX);

                    // Computes axis y based on Frames or Time
                    maxY = GetMaxYBasedOnFrameOrTime(file, maxY);

                    PlotModelData!.Series.Add(serie);

                }, token)
                .ConfigureAwait(false);

            // Adjust axis x based on max duration
            if (maxX > 0)
            {
                var axisX = PlotModelData!.Axes[0];
                axisX.AbsoluteMaximum = axisX.Maximum = maxX;
                axisX.StringFormat = AxisXStringFormatBuild(maxX);
            }

            // Adjust axis y based on Frames or Time
            if (maxY > 0)
            {
                var axisY = PlotModelData!.Axes[1];
                axisY.AbsoluteMaximum = axisY.Maximum = maxY;
            }

            PlotModelData!.InvalidatePlot(updateData: true);

        }

        private StairStepSeries GetNewSerie(string fileName)
            => new()
            {
                IsVisible = true,
                StrokeThickness = 1.5,
                Title = fileName,
                TrackerFormatString = TrackerFormatStringBuild,
                Decimator = Decimator.Decimate,
                LineJoin = LineJoin.Miter,
                VerticalStrokeThickness = 0.5,
                VerticalLineStyle = LineStyle.Dash,
                LineStyle = LineStyle.Solid,
                MarkerType = MarkerType.None,
                //Color = style.Color; 
            };

        private DataPoint GetDataPoint(FFProbePacket probePacket, FileItemViewModel file, PlotViewType plotViewType)
            => plotViewType switch
            {
                PlotViewType.FrameBased => new DataPoint((probePacket.PTSTime ?? 0) - file.StartTime, Convert.ToDouble(probePacket.Size) / 1000.0),
                //PlotViewType.SecondBased => new DataPoint(),
                _ => throw new NotImplementedException($"Text for {nameof(AxisYTitleBuild)} equals to {_plotViewType} is not implemented.")
            };

        private double GetMaxYBasedOnFrameOrTime(FileItemViewModel file, double maxY)
        {
            double? fileMaxY = _plotViewType switch
            {
                PlotViewType.FrameBased => file.Frames.Max(f => f.Size),
                PlotViewType.SecondBased => file.GetBitRateMaximum(),
                _ => throw new NotImplementedException($"Text for {nameof(AxisYTitleBuild)} equals to {_plotViewType} is not implemented.")
            } / 1000;
            if (fileMaxY != null && fileMaxY.Value > maxY)
            { maxY = fileMaxY.Value; }

            return maxY;
        }

        private static double GetMaxXBasedOnDuration(FileItemViewModel file, double maxX)
        {
            var fileDuration = file.GetDuration();
            if (fileDuration != null && fileDuration.Value > maxX)
            { maxX = fileDuration.Value; }

            return maxX;
        }

        [RelayCommand]
        private void Exit()
        {
            _appProcessService.Exit();
        }

    }

    public enum PlotViewType : int
    {
        FrameBased,
        SecondBased,
        GOPBased,
    }
}