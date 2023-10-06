using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.Services;

public class FileEntry
{
    private readonly IStorageFile _storageFile;

    public Uri Path { get => _storageFile.Path; }

    public Task<Stream> OpenReadAsync() => _storageFile.OpenReadAsync();

    public FileEntry(IStorageFile storageFile) => _storageFile = storageFile;
}