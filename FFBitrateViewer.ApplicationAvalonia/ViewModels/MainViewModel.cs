using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public string Greeting => "Welcome to Avalonia!";
    public ICommand ExitCommand { get; }

    public MainViewModel()
    {
        ExitCommand = new RelayCommand(ExitCommandHandler);
    }

    private void ExitCommandHandler()
    {
        DesktopApplication?.Shutdown();
    }

}
