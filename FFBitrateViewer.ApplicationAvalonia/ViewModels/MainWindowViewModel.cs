using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public partial class MainWindowViewModel(IServiceProvider serviceProvider) : ScreenViewModelBase
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    [RelayCommand]
    private void OnLoaded()
    {
        BitRateViewModel bitRateViewModel = _serviceProvider.GetService<BitRateViewModel>()!;
        Router.Navigate.Execute(bitRateViewModel);
    }
}