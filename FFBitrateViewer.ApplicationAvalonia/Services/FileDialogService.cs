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
        string title = "Open media file",
        bool isSingleFileSelection = true,
        params OpenFileFilterOption[]? filterOptions
    )
    {
        // Converts OpenFileFilterOption to FilePickerFileType
        filterOptions ??= [];
        var filePickerFileTypes = from filterOption in filterOptions
                                  select new FilePickerFileType(filterOption.Name)
                                  {
                                      AppleUniformTypeIdentifiers = filterOption.AppleUniformTypeIdentifiers,
                                      MimeTypes = filterOption.MimeTypes,
                                      Patterns = filterOption.Patters,
                                  };

        // Start async operation to open the dialog.
        FilePickerOpenOptions filePickerOpenOptions = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = !isSingleFileSelection,
            FileTypeFilter = filePickerFileTypes.ToArray()
        };
        var files = await ApplicationServices.Storage.OpenFilePickerAsync(filePickerOpenOptions).ConfigureAwait(false);

        if (files is null)
        { return ImmutableList<IFileEntry>.Empty; }

        return files.Select(f => new StorageFileEntry(f)).OfType<IFileEntry>().ToImmutableList();
    }

    public async Task<IFileEntry?> SaveAsync(
        string title, params SaveFileFilterOption[]? filterOptions
    )
    {

        // Converts SaveFilterOption to FilePickerFileType
        filterOptions ??= [];
        var filePickerFileTypes = from filterOption in filterOptions
                                  select new FilePickerFileType(filterOption.Name)
                                  {
                                      AppleUniformTypeIdentifiers = filterOption.AppleUniformTypeIdentifiers,
                                      MimeTypes = filterOption.MimeTypes,
                                      Patterns = filterOption.Patters,
                                  };

        // Start async operation to open the dialog.
        FilePickerSaveOptions filePickerSaveOptions = new FilePickerSaveOptions
        {
            Title = title,
            FileTypeChoices = filePickerFileTypes.ToArray()
        };
        var file = await ApplicationServices.Storage.SaveFilePickerAsync(filePickerSaveOptions).ConfigureAwait(false);
        if (file is not null)
        {
            return new StorageFileEntry(file);
        }

        return default;
    }
}

public record FileFilterOption(
        string? Name = null,
        string[]? Patters = null,
        string[]? MimeTypes = null,
        string[]? AppleUniformTypeIdentifiers = null);

public record OpenFileFilterOption(
        string? Name = null,
        string[]? Patters = null,
        string[]? MimeTypes = null,
        string[]? AppleUniformTypeIdentifiers = null)
    : FileFilterOption(Name, Patters, MimeTypes, AppleUniformTypeIdentifiers);

public record SaveFileFilterOption(
        string? Name = null,
        string[]? Patters = null,
        string[]? MimeTypes = null,
        string[]? AppleUniformTypeIdentifiers = null)
    : FileFilterOption(Name, Patters, MimeTypes, AppleUniformTypeIdentifiers);


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