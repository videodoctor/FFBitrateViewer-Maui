using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using System;

namespace FFBitrateViewer.ApplicationAvalonia.Services;

internal static class ApplicationServices
{
    internal static IClassicDesktopStyleApplicationLifetime Desktop => _desktop.Value;

    private static readonly Lazy<IClassicDesktopStyleApplicationLifetime> _desktop = new(
        () =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime instance)
            { return instance; }
            throw new InvalidApplicationDesktopOperationException($"Only {nameof(IClassicDesktopStyleApplicationLifetime)} type is supported for the current application.");
        });

    internal static TopLevel MainWindowTopLevel
    {
        get
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(Desktop.MainWindow);
            return topLevel is null
                ? throw new InvalidApplicationDesktopOperationException($"Error accessing {nameof(TopLevel)} of {nameof(Desktop)}.{nameof(Desktop.MainWindow)}.")
                : topLevel;
        }
    }

    internal static IStorageProvider Storage => MainWindowTopLevel.StorageProvider;

    internal static IClipboard Clipboard => MainWindowTopLevel.Clipboard is null
                ? throw new InvalidApplicationDesktopOperationException($"Error accessing {nameof(Clipboard)}.")
                : MainWindowTopLevel.Clipboard;

}

[Serializable]
public class InvalidApplicationDesktopOperationException : FFBitrateViewerException
{
    public InvalidApplicationDesktopOperationException() { }
    public InvalidApplicationDesktopOperationException(string message) : base(message) { }
    public InvalidApplicationDesktopOperationException(string message, Exception inner) : base(message, inner) { }
}