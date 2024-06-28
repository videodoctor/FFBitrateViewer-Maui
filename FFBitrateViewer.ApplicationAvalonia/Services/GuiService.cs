using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.Services;


public class GuiService
{
    internal static IClassicDesktopStyleApplicationLifetime? DesktopApplication => _desktopApplication.Value;

    private static readonly Lazy<IClassicDesktopStyleApplicationLifetime?> _desktopApplication = new(() => Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);

    public void RunLater(Action action)
        => Dispatcher.UIThread.Post(action, DispatcherPriority.Default);

    public void RunLater<T>(Action<T> action, T? state)
        => Dispatcher.UIThread.Post( state => {
            if (state is not T typedState)
            {
                throw new InvalidOperationException($"Expect {nameof(state)} argument to be of type {typeof(T).FullName}");
            }
            action(typedState);
        }, state, DispatcherPriority.Default);

    public async Task RunNowAsync(Action action)
        => await Dispatcher.UIThread.InvokeAsync(action, DispatcherPriority.Default);

    public void RunNow(Action action)
        => Dispatcher.UIThread.Invoke(action, DispatcherPriority.Default);

    public void Exit(int exitCode = 0)
    {
        _desktopApplication.Value?.Shutdown(exitCode);
    }

}