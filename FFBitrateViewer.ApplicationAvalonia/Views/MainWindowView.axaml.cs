using Avalonia.ReactiveUI;
using FFBitrateViewer.ApplicationAvalonia.ViewModels;

namespace FFBitrateViewer.ApplicationAvalonia.Views;

public partial class MainWindowView : ReactiveWindow<MainWindowViewModel>
{
    public MainWindowView()
    {
        InitializeComponent();
    }
}