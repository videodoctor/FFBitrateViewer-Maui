using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.Services;

public class GuiService
{
    internal static IClassicDesktopStyleApplicationLifetime? DesktopApplication => _desktopApplication.Value;

    private static readonly Lazy<IClassicDesktopStyleApplicationLifetime?> _desktopApplication = new(() => Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);

    /// <summary>
    /// Executes the specified action on the UI thread asynchronously.
    /// </summary>
    /// <param name="action">The action to be executed.</param>
    /// <remarks>
    /// This method uses <see cref="Avalonia.Threading.Dispatcher.UIThread.Post(System.Action, Avalonia.Threading.DispatcherPriority)"/> 
    /// to schedule the action to be executed on the UI thread.
    /// </remarks>
    public void RunLater(Action action)
        => Dispatcher.UIThread.Post(action, DispatcherPriority.Default);

    /// <summary>
    /// Executes the specified action on the UI thread asynchronously.
    /// </summary>
    /// <param name="action">The action to be executed.</param>
    /// <remarks>
    /// This method uses <see cref="Avalonia.Threading.Dispatcher.UIThread.Post(System.Action, Avalonia.Threading.DispatcherPriority)"/> 
    /// to schedule the action to be executed on the UI thread.
    /// </remarks>
    public void RunLater<T>(Action<T> action, T? state)
        => Dispatcher.UIThread.Post(state => {
            if (state is not T typedState)
            {
                throw new InvalidOperationException($"Expect {nameof(state)} argument to be of type {typeof(T).FullName}");
            }
            action(typedState);
        }, state, DispatcherPriority.Default);

    /// <summary>
    /// Executes the specified action on the UI thread synchronously.
    /// </summary>
    /// <param name="action">The action to be executed.</param>
    /// <remarks>
    /// This method uses <see cref="Avalonia.Threading.Dispatcher.UIThread.Post(System.Action, Avalonia.Threading.DispatcherPriority)"/> 
    /// to schedule the action to be executed on the UI thread.
    /// </remarks>
    public async Task RunNowAsync(Action action)
        => await Dispatcher.UIThread.InvokeAsync(action, DispatcherPriority.Default);

    /// <summary>
    /// Executes the specified action on the UI thread synchronously.
    /// </summary>
    /// <param name="action">The action to be executed.</param>
    /// <remarks>
    /// This method uses <see cref="Avalonia.Threading.Dispatcher.UIThread.Post(System.Action, Avalonia.Threading.DispatcherPriority)"/> 
    /// to schedule the action to be executed on the UI thread.
    /// </remarks>
    public void RunNow(Action action)
        => Dispatcher.UIThread.Invoke(action, DispatcherPriority.Default);

    /// <summary>
    /// Exits the application with the specified exit code.
    /// </summary>
    /// <param name="exitCode">The exit code to be returned to the operating system. The default value is 0.</param>
    /// <remarks>
    /// This method uses the <see cref="IClassicDesktopStyleApplicationLifetime.Shutdown(int)"/> method to exit the application.
    /// If the <see cref="DesktopApplication"/> is null, this method does nothing.
    /// </remarks>
    public void Exit(int exitCode = 0)
    {
        _desktopApplication.Value?.Shutdown(exitCode);
    }

    /// <summary>
    /// Whether or not the GUI is using Dark theme.
    /// </summary>
    public bool IsDarkTheme => string.Equals("Dark", TopLevel.GetTopLevel(_desktopApplication.Value!.MainWindow)!.ActualThemeVariant.Key.ToString(), StringComparison.OrdinalIgnoreCase);

}