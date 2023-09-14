using FFBitrateViewer.ApplicationAvalonia.Services;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public class FileItemViewModel : ViewModelBase
{
    private readonly FileEntry _fileEntry;

    public FileItemViewModel(FileEntry fileEntry)
    {
        _fileEntry = fileEntry;
    }
}
