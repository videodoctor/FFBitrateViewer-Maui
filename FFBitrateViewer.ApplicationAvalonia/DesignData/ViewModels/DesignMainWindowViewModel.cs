using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.ViewModels;
using System.Threading;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.DesignData.ViewModels;

public partial class DesignMainWindowViewModel : ScreenViewModelBase
{
    [RelayCommand]
    private async Task OnLoaded(CancellationToken token)
    { await Task.Yield(); }

}