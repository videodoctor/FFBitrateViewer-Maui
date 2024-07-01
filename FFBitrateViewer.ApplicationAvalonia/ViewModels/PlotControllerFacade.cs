using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public class PlotControllerFacade(
    IPlotControl? plotControl = null,
    IPlotStrategy? plotStrategy = null
) {
    internal static readonly PlotControllerFacade None = new();

    public IPlotControl? PlotController { get; private set; } = plotControl;

    public IPlotStrategy PlotStrategy { get; private set; } = plotStrategy ?? NonePlotStrategy.Instance;

    public string AxisYTitleLabel
    {
        get => PlotController?.Plot.Axes.Left.Label.Text ?? string.Empty;
        set { if (PlotController is not null) { PlotController.Plot.Axes.Left.Label.Text = value; } }
    }
    private static readonly object _newScatterLock = new ();
    
    private Crosshair? MyCrosshair;
    private Marker? MyHighlightMarker;
    private Text? MyHighlightText;

    //private string TrackerFormatStringBuild => $@"{{0}}{Environment.NewLine}Time={{2:hh\:mm\:ss\.fff}}{Environment.NewLine}{{3}}={{4:0}} "; //{PlotStrategy.AxisYTickLabelSuffix}

    public IPlottable? InsertScatter(
        List<double> xs,
        List<int> ys,
        string legendText,
        ConnectStyle connectStyle = ConnectStyle.StepHorizontal
    )
    {
        if (PlotController is null)
        { return null; }

        // NOTE: make thread safe scatter creation thus automatically color assignment do not reuse color.
        Scatter scatter;
        lock (_newScatterLock) { scatter = PlotController.Plot.Add.Scatter(xs, ys); }
        scatter.ConnectStyle = connectStyle;
        scatter.LegendText = legendText;

        return scatter;
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

        if(IsDarkThemeEnable)
        {
            SetDarkTheme();
        }

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
            Edge = Edge.Right,
            Alignment = Alignment.UpperCenter,
        };

        PlotController.Plot.Axes.AddPanel(pan);

        // Customize grid with sublines
        PlotController.Plot.Grid.MajorLineColor = Colors.Green.WithOpacity(.5);
        PlotController.Plot.Grid.MinorLineColor = Colors.Green.WithOpacity(.1);
        PlotController.Plot.Grid.MinorLineWidth = 1;

        // Makes auto scale to be tight
        PlotController.Plot.Axes.Margins(0, 0);

        // Create a marker to highlight the point under the cursor
        MyCrosshair = PlotController.Plot.Add.Crosshair(0, 0);
        MyHighlightMarker = PlotController.Plot.Add.Marker(0, 0);
        MyHighlightMarker.Shape = MarkerShape.OpenCircle;
        MyHighlightMarker.Size = 17;
        MyHighlightMarker.LineWidth = 2;

        // Create a text label to place near the highlighted value
        MyHighlightText = PlotController.Plot.Add.Text(string.Empty, 0, 0);
        MyHighlightText.LabelAlignment = Alignment.LowerLeft;
        MyHighlightText.LabelBold = true;
        MyHighlightText.OffsetX = 7;
        MyHighlightText.OffsetY = -7;

    }

    public void Refresh()
        => PlotController?.Refresh();

    public void HandleMouseMoved(Avalonia.Input.PointerEventArgs pointerEventArgs)
    {
        // Prevents handling if it cannot draw the mark
        if (pointerEventArgs.Handled || MyCrosshair is null || MyHighlightText is null || MyHighlightMarker is null)
        {  return; }

        // Get the control that raised the event
        var avaPlot = (ScottPlot.Avalonia.AvaPlot)pointerEventArgs.Source!;

        // Get the position relative to the control
        var position = pointerEventArgs.GetPosition(avaPlot);

        // determine where the mouse is
        Pixel mousePixel = new(position.X, position.Y);
        Coordinates mouseLocation = avaPlot.Plot.GetCoordinates(mousePixel);

        // get the nearest point of each scatter
        Dictionary<int, DataPoint> nearestPoints = new();
        var MyScatters = avaPlot.Plot.PlottableList.OfType<Scatter>().ToList();
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

            MyCrosshair.IsVisible = true;
            MyCrosshair.Position = dataPoint.Coordinates;
            MyCrosshair.LineColor = scatter.MarkerStyle.FillColor;

            MyHighlightMarker.IsVisible = true;
            MyHighlightMarker.Location = dataPoint.Coordinates;
            MyHighlightMarker.MarkerStyle.LineColor = scatter.MarkerStyle.FillColor;

            MyHighlightText.IsVisible = true;
            MyHighlightText.Location = dataPoint.Coordinates;
            MyHighlightText.LabelText =$"{PlotStrategy.AxisXTickLabelPrefix}={PlotStrategy.AxisXValueToString(dataPoint.X)}{PlotStrategy.AxisXTickLabelSuffix}{Environment.NewLine}{PlotStrategy.AxisYTickLabelPrefix}={PlotStrategy.AxisYValueToString(dataPoint.Y)}{PlotStrategy.AxisYTickLabelSuffix}";
            MyHighlightText.LabelFontColor = scatter.MarkerStyle.FillColor;

            avaPlot.Refresh();
            //string text = $"Selected Scatter={scatter.LegendText}, Index={point.Index}, X={point.X:0.##}, Y={point.Y:0.##}";
            //Debug.WriteLine(text);
        }

        // hide the crosshair, marker and text when no point is selected
        if (!pointSelected && MyCrosshair.IsVisible)
        {
            MyCrosshair.IsVisible = false;
            MyHighlightMarker.IsVisible = false;
            MyHighlightText.IsVisible = false;
            avaPlot.Refresh();
            //string text = $"No point selected";
            //Debug.WriteLine(text);
        }
    }

}
