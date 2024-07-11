using Avalonia.ReactiveUI;
using FFBitrateViewer.ApplicationAvalonia.ViewModels;

namespace FFBitrateViewer.ApplicationAvalonia.Views;

public partial class BitRateView : ReactiveUserControl<BitRateViewModel>
{
    public BitRateView()
    {
        InitializeComponent();
    }
}