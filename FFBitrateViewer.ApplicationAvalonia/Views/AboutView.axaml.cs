using Avalonia.ReactiveUI;
using FFBitrateViewer.ApplicationAvalonia.ViewModels;

namespace FFBitrateViewer.ApplicationAvalonia.Views;

public partial class AboutView : ReactiveUserControl<AboutViewModel>
{
    public AboutView()
    {
        InitializeComponent();
    }
}