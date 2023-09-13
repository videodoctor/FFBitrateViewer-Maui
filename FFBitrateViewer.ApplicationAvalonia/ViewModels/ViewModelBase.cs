using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public class ViewModelBase : ObservableObject
{
    public IClassicDesktopStyleApplicationLifetime? DesktopApplication => _desktopApplication.Value;
    private static Lazy<IClassicDesktopStyleApplicationLifetime?> _desktopApplication
        => new Lazy<IClassicDesktopStyleApplicationLifetime?>(() => Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);

}
