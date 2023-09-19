using Avalonia.Controls;
namespace FFBitrateViewer.ApplicationAvalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Loaded += MainView_Loaded;
    }

    private void MainView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.MainViewModel mainViewModel)
        { return; }

        mainViewModel.PlotControllerData = PlotControl.ActualController;
        mainViewModel.PlotModelData = PlotControl.ActualModel;

    }
}
