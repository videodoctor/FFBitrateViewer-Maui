using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public partial class MainWindowViewModel (IServiceProvider serviceProvider) : ScreenViewModelBase
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    [RelayCommand]
    private void OnLoaded()
    { 
        BitRateViewModel bitRateViewModel = _serviceProvider.GetService<BitRateViewModel>()!;
        Router.Navigate.Execute(bitRateViewModel);
    }
}
