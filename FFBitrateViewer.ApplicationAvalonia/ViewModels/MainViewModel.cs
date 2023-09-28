using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.ffprobe;
using OxyPlot;
using System;
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

            var version = await _ffprobeAppClient.GetVersionAsync();
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
            var fileInfoEntries = await _fileDialogService.OpenAsync(IsSingleSelection: false);
            for (int fileInfoIndex = 0; fileInfoIndex < fileInfoEntries.Count; fileInfoIndex++)
            {
                var fileInfo = fileInfoEntries[fileInfoIndex];
                var mediaInfo = await _ffprobeAppClient.GetMediaInfoAsync(fileInfo.Path.LocalPath);
                var fileItemViewModel = new FileItemViewModel(fileInfo, mediaInfo) { IsSelected = true };

                // Add file to Data Grid
                Files.Add(fileItemViewModel);
            }
            SelectedFile = Files.LastOrDefault();
            PlotModelData!.InvalidatePlot(updateData: true);
        }

        [RelayCommand(IncludeCancelCommand = true, FlowExceptionsToTaskScheduler = true)]
        private async Task ToggleOnOffPlotterPlotter(CancellationToken token)
        {
            PlotModelData!.Series.Clear();

            for (int fileIndex = 0; fileIndex < Files.Count; fileIndex++)
            {
                var file = Files[fileIndex];
                if (!file.IsSelected) { continue; }
                await ProcessFileAsync(file, token);
            }

            PlotModelData!.InvalidatePlot(updateData: true);

        }

        private async Task ProcessFileAsync(FileItemViewModel file, CancellationToken token)
        {
            // Add Series to data grid
            var serie = new OxyPlot.Series.StairStepSeries
            {
                IsVisible = true,
                StrokeThickness = 1.5,
                Title = Path.GetFileName(file.Path.LocalPath),
                TrackerFormatString = TrackerFormatStringBuild,
                Decimator = Decimator.Decimate,
                LineJoin = LineJoin.Miter,
                VerticalStrokeThickness = 0.5,
                VerticalLineStyle = LineStyle.Dash,
                LineStyle = LineStyle.Solid,
                MarkerType = MarkerType.None,
                //Color = style.Color; 
            };


            // Request ffprobe information to create data points for serie
            await foreach (var probePacket in _ffprobeAppClient.GetProbePackets(file.Path.LocalPath, token: token))
            {
                token.ThrowIfCancellationRequested();

                // TODO: We need to keep track of max duration per file and amogn all files
                //       In order to adjust: axis.AbsoluteMaximum = axis.Maximum 

                //return new Frame()
                //{
                //    Duration = (double)packet.DurationTime,
                //    FrameType = packet.Flags?.IndexOf("K") >= 0 ? FramePictType.I : null,
                //    IsOrdered = false, // 'Packets' returned by FFProbe are ordered by DTS, not PTS so will need to order them later when adding onto list
                //    Size = (int)packet.Size,
                //    StartTime = (double)packet.PTSTime
                //};

                var dataPoint = _plotViewType switch
                {
                    PlotViewType.FrameBased => new DataPoint((probePacket.PTSTime ?? 0) - file.StartTime, Convert.ToDouble(probePacket.Size) / 1000.0),
                    //PlotViewType.SecondBased => new DataPoint(),
                    _ => throw new NotImplementedException($"Text for {nameof(AxisYTitleBuild)} equals to {_plotViewType} is not implemented.")
                };

                serie.Points.Add(dataPoint);
            }
            PlotModelData!.Series.Add(serie);
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