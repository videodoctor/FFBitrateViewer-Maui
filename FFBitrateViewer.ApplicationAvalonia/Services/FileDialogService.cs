using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.Services;

public class FileDialogService
{
    public async Task<IList<IFileEntry>> OpenAsync(
        string filePickerTitle = "Open Text File",
        bool IsSingleSelection = true
    )
    {
        if (GuiService.DesktopApplication is null)
        { throw new FileDialogException("A desktop application is required to open files."); }

        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(GuiService.DesktopApplication.MainWindow);

        // Start async operation to open the dialog.
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = filePickerTitle,
            AllowMultiple = !IsSingleSelection
        });


        return files.Select(f => new StorageFileEntry(f)).OfType<IFileEntry>().ToImmutableList();
    }

}

public class StorageFileEntry(IStorageFile storageFile) : IFileEntry
{
    private readonly IStorageFile _storageFile = storageFile;

    public Uri Path { get => _storageFile.Path; }

    public Task<Stream> OpenReadAsync() => _storageFile.OpenReadAsync();
}

public class LocalFileEntry(string filePath) : IFileEntry
{
    public Uri Path { get; } = new Uri(filePath, UriKind.RelativeOrAbsolute);

    public Task<Stream> OpenReadAsync() => Task.FromResult((Stream)File.OpenRead(Path.LocalPath));
}

[Serializable]
public class FileDialogException : FFBitrateViewerException
{
    public FileDialogException() { }
    public FileDialogException(string message) : base(message) { }
    public FileDialogException(string message, System.Exception inner) : base(message, inner) { }
}
