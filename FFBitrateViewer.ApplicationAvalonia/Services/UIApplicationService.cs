﻿using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.Services;


public class UIApplicationService
{
    internal static IClassicDesktopStyleApplicationLifetime? DesktopApplication => _desktopApplication.Value;

    private static readonly Lazy<IClassicDesktopStyleApplicationLifetime?> _desktopApplication = new(() => Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);

    public void FireAndForget(Action action)
        => Dispatcher.UIThread.Post(action, DispatcherPriority.Default);

    public void FireAndForget<T>(Action<T> action, T? state)
        => Dispatcher.UIThread.Post( state => {
            if (state is not T typedState)
            {
                throw new InvalidOperationException($"Expect {nameof(state)} argument to be of type {typeof(T).FullName}");
            }
            action(typedState);
        }, state, DispatcherPriority.Default);

    public async Task ExecuteAsync(Action action)
        => await Dispatcher.UIThread.InvokeAsync(action, DispatcherPriority.Default);

    public void Execute(Action action)
        => Dispatcher.UIThread.Invoke(action, DispatcherPriority.Default);

    public void Exit(int exitCode = 0)
    {
        _desktopApplication.Value?.Shutdown(exitCode);
    }

}