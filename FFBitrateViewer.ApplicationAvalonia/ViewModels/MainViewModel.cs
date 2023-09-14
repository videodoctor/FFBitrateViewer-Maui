using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public string Greeting => "Welcome to Avalonia!";
    public ICommand ExitCommand { get; }

    public ICommand AddFilesCommand { get; }

    public ObservableCollection<FileItemViewModel> Files { get; }

    private readonly AppProcessService _appProcessService;
    private readonly FileDialogService _fileDialogService;

    public MainViewModel()
    {
        _appProcessService = new AppProcessService();
        _fileDialogService = new FileDialogService();

        Files = new ObservableCollection<FileItemViewModel>();

        ExitCommand = new RelayCommand(ExitCommandHandler);
        AddFilesCommand = new RelayCommand(AddFilesCommandHandler);

    }

    private async void AddFilesCommandHandler()
    {
        var fileEntries = await _fileDialogService.OpenAsync(IsSingleSelection: false);
        foreach (var fileEntry in fileEntries)
        {
            Files.Add(new FileItemViewModel(fileEntry));
        }
    }

    private void ExitCommandHandler()
    {
        _appProcessService.Exit();
    }

}
