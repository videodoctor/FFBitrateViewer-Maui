using FFBitrateViewer.ApplicationAvalonia.Extensions;
using FFBitrateViewer.ApplicationAvalonia.Models.Media;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public class PlotControllerFacade(
    IEnumerable<IPlotStrategy> plotStrategies
)
{
    internal static System.Drawing.Color TransparentColor = Colors.Transparent.ToDrawingColor();

    public IPlotControl? PlotController { get; set; }
    public PlotViewType PlotView { get; set; } = PlotViewType.FrameBased;
    public IPlotStrategy PlotStrategy => _plotStrategies[PlotView];

    private readonly IDictionary<PlotViewType, IPlotStrategy> _plotStrategies = plotStrategies.ToDictionary(p => p.PlotViewType);

    public string AxisYTitleLabel
    {
        get => PlotController?.Plot.Axes.Left.Label.Text ?? string.Empty;
        set { if (PlotController is not null) { PlotController.Plot.Axes.Left.Label.Text = value; } }
    }
    private static readonly object _newScatterLock = new();

    private Crosshair? _markerCrosshair;
    private Marker? _markerHighlightMarker;
    private Text? _markerHighlightText;

    public (IPlottable? plottable, System.Drawing.Color scatterLineColor) InsertScatter(
        List<double> xs,
        List<int> ys,
        string legendText,
        ConnectStyle connectStyle = ConnectStyle.StepHorizontal
    )
    {
        if (PlotController is null)
        { return (null, TransparentColor); }

        // NOTE: make thread safe scatter creation thus automatically color assignment do not reuse color.
        Scatter scatter;
        lock (_newScatterLock) { scatter = PlotController.Plot.Add.Scatter(xs, ys); }
        scatter.ConnectStyle = connectStyle;
        scatter.LegendText = legendText;

        return (scatter, scatter.LineColor.ToDrawingColor());
    }

    public void RemoveScatter(IPlottable? plottable)
    {
        if (plottable is null)
        { return; }

        PlotController?.Plot.Remove(plottable);
    }

    public void AutoScaleViewport()
        => PlotController?.Plot.Axes.AutoScale();

    public void SetDarkTheme()
    {
        if (PlotController is null)
        { return; }

        PlotController.Plot.Add.Palette = new ScottPlot.Palettes.Penumbra();
        // change figure colors
        PlotController.Plot.FigureBackground.Color = Color.FromHex("#181818");
        PlotController.Plot.DataBackground.Color = Color.FromHex("#1f1f1f");

        // change axis and grid colors
        PlotController.Plot.Axes.Color(Color.FromHex("#d7d7d7"));
        PlotController.Plot.Grid.MajorLineColor = Color.FromHex("#404040");

        // change legend colors
        PlotController.Plot.Legend.BackgroundColor = Color.FromHex("#404040");
        PlotController.Plot.Legend.FontColor = Color.FromHex("#d7d7d7");
        PlotController.Plot.Legend.OutlineColor = Color.FromHex("#d7d7d7");

        // Customize grid with sublines
        PlotController.Plot.Grid.MajorLineColor = Colors.LightGreen.WithOpacity(.5);
        PlotController.Plot.Grid.MinorLineColor = Colors.LightGreen.WithOpacity(.1);
        PlotController.Plot.Grid.MinorLineWidth = 1;
    }

    public void Initialize(string axisYTitleLabel, bool IsDarkThemeEnable)
    {
        if (PlotController is null)
        { return; }

        if (IsDarkThemeEnable)
        {
            SetDarkTheme();
        }

        // default plot title
        PlotController.Plot.Title("No point selected");

        // Showing the left title
        PlotController.Plot.Axes.Left.Label.Text = axisYTitleLabel;

        // create a custom tick generator using your custom label formatter
        ScottPlot.TickGenerators.NumericAutomatic myTickGenerator = new()
        {
            LabelFormatter = PlotStrategy.AxisXValueToString
        };
        PlotController.Plot.Axes.Bottom.TickGenerator = myTickGenerator;

        // hide the default legend
        PlotController.Plot.HideLegend();

        // display the legend in a LegendPanel outside the plot
        ScottPlot.Panels.LegendPanel pan = new(PlotController.Plot.Legend)
        {
            Edge = Edge.Bottom,
            Alignment = Alignment.MiddleCenter,
        };
        pan.Legend.Orientation = Orientation.Horizontal;

        PlotController.Plot.Axes.AddPanel(pan);

        // Customize grid with sublines
        PlotController.Plot.Grid.MajorLineColor = Colors.Green.WithOpacity(.5);
        PlotController.Plot.Grid.MinorLineColor = Colors.Green.WithOpacity(.1);
        PlotController.Plot.Grid.MinorLineWidth = 1;

        // Makes auto scale to be tight
        // PlotController.Plot.Axes.Margins(0, 0, 0, 0);

        // Create a marker to highlight the point under the cursor
        _markerCrosshair = PlotController.Plot.Add.Crosshair(0, 0);
        _markerHighlightMarker = PlotController.Plot.Add.Marker(0, 0);
        _markerHighlightMarker.Shape = MarkerShape.OpenCircle;
        _markerHighlightMarker.Size = 17;
        _markerHighlightMarker.LineWidth = 2;

        // Create a text label to place near the highlighted value
        _markerHighlightText = PlotController.Plot.Add.Text(string.Empty, 0, 0);
        _markerHighlightText.LabelAlignment = Alignment.LowerLeft;
        _markerHighlightText.LabelBold = true;
        _markerHighlightText.OffsetX = 7;
        _markerHighlightText.OffsetY = -7;

    }

    public void Refresh()
        => PlotController?.Refresh();

    public void HandleMouseMoved(Avalonia.Input.PointerEventArgs pointerEventArgs)
    {
        // Prevents handling if it cannot draw the mark
        if (pointerEventArgs.Handled || _markerCrosshair is null || _markerHighlightText is null || _markerHighlightMarker is null)
        { return; }

        // Get the control that raised the event
        var avaPlot = (ScottPlot.Avalonia.AvaPlot)pointerEventArgs.Source!;

        // Get the position relative to the control
        var position = pointerEventArgs.GetPosition(avaPlot);

        // determine where the mouse is
        Pixel mousePixel = new(position.X, position.Y);
        Coordinates mouseLocation = avaPlot.Plot.GetCoordinates(mousePixel);

        // get the nearest point of each scatter
        Dictionary<int, DataPoint> nearestPoints = [];
        var MyScatters = avaPlot.Plot.PlottableList.OfType<Scatter>().Where(s => s.IsVisible).ToList();
        for (int i = 0; i < MyScatters.Count; i++)
        {
            DataPoint nearestPoint = MyScatters[i].Data.GetNearest(mouseLocation, avaPlot.Plot.LastRender);
            nearestPoints.Add(i, nearestPoint);
        }

        // determine which scatter's nearest point is nearest to the mouse
        bool pointSelected = false;
        int scatterIndex = -1;
        double smallestDistance = double.MaxValue;
        for (int i = 0; i < nearestPoints.Count; i++)
        {
            if (nearestPoints[i].IsReal)
            {
                // calculate the distance of the point to the mouse
                double distance = nearestPoints[i].Coordinates.Distance(mouseLocation);
                if (distance < smallestDistance)
                {
                    // store the index
                    scatterIndex = i;
                    pointSelected = true;
                    // update the smallest distance
                    smallestDistance = distance;
                }
            }
        }

        // place the crosshair, marker and text over the selected point
        if (pointSelected)
        {
            ScottPlot.Plottables.Scatter scatter = MyScatters[scatterIndex];
            DataPoint dataPoint = nearestPoints[scatterIndex];

            _markerCrosshair.IsVisible = true;
            _markerCrosshair.Position = dataPoint.Coordinates;
            _markerCrosshair.LineColor = scatter.MarkerStyle.FillColor;

            _markerHighlightMarker.IsVisible = true;
            _markerHighlightMarker.Location = dataPoint.Coordinates;
            _markerHighlightMarker.MarkerStyle.LineColor = scatter.MarkerStyle.FillColor;

            _markerHighlightText.IsVisible = true;
            _markerHighlightText.Location = dataPoint.Coordinates;
            string seriesDescription = $"Filename: {scatter.LegendText}";
            string axisXDescription = $"{PlotStrategy.AxisXTickLabelPrefix}: {PlotStrategy.AxisXValueToString(dataPoint.X)}{PlotStrategy.AxisXTickLabelSuffix}";
            string axisYDescription = $"{PlotStrategy.AxisYTickLabelPrefix}: {PlotStrategy.AxisYValueToString(dataPoint.Y)} {PlotStrategy.AxisYTickLabelSuffix}";
            _markerHighlightText.LabelText = $"{seriesDescription}{Environment.NewLine}{axisXDescription}{Environment.NewLine}{axisYDescription}";
            _markerHighlightText.LabelFontColor = scatter.MarkerStyle.FillColor;
            _markerHighlightText.LabelBackgroundColor = Colors.Black.WithAlpha(128);

            avaPlot.Refresh();
            //string text = $"Selected Scatter={scatter.LegendText}, Index={point.Index}, X={point.X:0.##}, Y={point.Y:0.##}";
            //Debug.WriteLine(text);
            PlotController?.Plot.Title($"{seriesDescription} {axisXDescription} {axisYDescription}");
        }

        // hide the crosshair, marker and text when no point is selected
        if (!pointSelected && _markerCrosshair.IsVisible)
        {
            _markerCrosshair.IsVisible = false;
            _markerHighlightMarker.IsVisible = false;
            _markerHighlightText.IsVisible = false;
            avaPlot.Refresh();
            //string text = $"No point selected";
            //Debug.WriteLine(text);
            PlotController?.Plot.Title("No point selected");

        }
    }

    public void SavePlotImage(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        ImageFormat imageFormat = ImageFormatLookup.FromFileExtension(extension);
        PlotController?.Plot.Save(filePath, 1920, 1080, imageFormat);
    }

    public byte[]? GetPlotImageAsStream()
    {
        if (PlotController is null)
        { return default; }

        PixelSize lastRenderSize = PlotController.Plot.RenderManager.LastRender.FigureRect.Size;
        Image bmp = PlotController.Plot.GetImage((int)lastRenderSize.Width, (int)lastRenderSize.Height);
        byte[] bmpBytes = bmp.GetImageBytes();

        return bmpBytes;
    }
}