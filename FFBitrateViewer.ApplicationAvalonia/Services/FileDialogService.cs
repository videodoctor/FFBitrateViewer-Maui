﻿using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.Services;

public class FileDialogService
{
    public async Task<IList<FileEntry>> OpenAsync(
        string filePickerTitle = "Open Text File",
        bool IsSingleSelection = true
    )
    {
        if (UIApplicationService.DesktopApplication is null)
        { throw new System.Exception("A desktop application is required to open files."); }

        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(UIApplicationService.DesktopApplication.MainWindow);

        // Start async operation to open the dialog.
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = filePickerTitle,
            AllowMultiple = !IsSingleSelection
        });


        return files.Select(f => new FileEntry(f)).ToImmutableList();
    }

}