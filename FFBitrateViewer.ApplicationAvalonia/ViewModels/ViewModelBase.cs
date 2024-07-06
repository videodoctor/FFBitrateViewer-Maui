using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using System.ComponentModel;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public class ViewModelBase : ObservableObject
{

}

public class RoutableViewModelBase(IScreen screen, string? urlPathSegment = null) : ViewModelBase, IRoutableViewModel
{
    public string? UrlPathSegment { get; set; } = urlPathSegment ?? string.Empty;

    public IScreen HostScreen { get; set; } = screen;

    public void RaisePropertyChanged(PropertyChangedEventArgs args)
        => OnPropertyChanged(args);

    public void RaisePropertyChanging(PropertyChangingEventArgs args)
        => OnPropertyChanging(args);
}

public class ScreenViewModelBase : ViewModelBase, IScreen
{
    public RoutingState Router { get; } = new();
}