using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public class PlotControllerFacade(IPlotControl? plotControl = null)
{
    internal static readonly PlotControllerFacade None = new();

    public IPlotControl? PlotController { get; private set; } = plotControl;

    public string AxisYTitleLabel
    {
        get => PlotController?.Plot.Axes.Left.Label.Text ?? string.Empty;
        set { if (PlotController is not null) { PlotController.Plot.Axes.Left.Label.Text = value; } }
    }
    private static readonly object _newScatterLock = new ();

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
            LabelFormatter = AxisXTickLabelFormatter
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

    }

    public void Refresh()
        => PlotController?.Refresh();

    private string AxisXTickLabelFormatter(double duration) => TimeSpan.FromSeconds(duration).ToString("g");
}
