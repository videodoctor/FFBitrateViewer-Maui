using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.Services;
using System;
using System.IO;
using System.Windows.Input;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public string Greeting => "Welcome to Avalonia!";
    public ICommand ExitCommand { get; }

    public ICommand AddFilesCommand { get; }

    private readonly AppProcessService _appProcessService;
    private readonly FileDialogService _fileDialogService;

    public MainViewModel()
    {
        _appProcessService = new AppProcessService();
        _fileDialogService = new FileDialogService();

        ExitCommand = new RelayCommand(ExitCommandHandler);
        AddFilesCommand = new RelayCommand(AddFilesCommandHandler);

    }

    private async void AddFilesCommandHandler()
    {
        var fileEntries = await _fileDialogService.OpenAsync(IsSingleSelection: false);

    }

    private void ExitCommandHandler()
    {
        _appProcessService.Exit();
    }

}
