using CommunityToolkit.Mvvm.Input;
using ReactiveUI;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public partial class AboutViewModel(IScreen screen) : RoutableViewModelBase(screen, nameof(AboutViewModel))
{

    [RelayCommand]
    private void GoBack()
    {
        if (HostScreen.Router.NavigationStack.Count == 0)
        { return; }

        HostScreen.Router.NavigateBack.Execute();
    }
}
