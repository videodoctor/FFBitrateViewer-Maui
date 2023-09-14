using CommunityToolkit.Mvvm.ComponentModel;
using FFBitrateViewer.ApplicationAvalonia.Services;
using System;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public partial class FileItemViewModel : ViewModelBase
{

    [ObservableProperty]
    private bool _isSelected;

    public Uri Path { get => _fileEntry.Path;  }

    private readonly FileEntry _fileEntry;

    public FileItemViewModel(FileEntry fileEntry)
    {
        _fileEntry = fileEntry;
    }
}
