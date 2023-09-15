using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.Extensibility.OxyPlot;
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.ffprobe;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _version = string.Empty;

    public ICommand ExitCommand { get; }

    public ICommand AddFilesCommand { get; }

    public ICommand OnLoadedCommand { get; }

    public ObservableCollection<FileItemViewModel> Files { get; }
    public FFProbePlotModel PlotModel { get; }

    private readonly AppProcessService _appProcessService;
    private readonly FileDialogService _fileDialogService;
    private readonly FFProbeAppClient _ffprobeAppClient;

    public MainViewModel()
    {
        _appProcessService = new();
        _fileDialogService = new();
        _ffprobeAppClient = new();

        Files = new();
        PlotModel = new(string.Empty);

        ExitCommand = new RelayCommand(ExitCommandHandler);
        AddFilesCommand = new RelayCommand(AddFilesCommandHandler);
        OnLoadedCommand = new AsyncRelayCommand(OnLoadedCommandHandler);

    }

    private async Task OnLoadedCommandHandler()
    {
        var version = await _ffprobeAppClient.GetVersionAsync();
        Version = $"{System.IO.Path.GetFileName(_ffprobeAppClient.FFProbeFilePath)} v{version}";
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
