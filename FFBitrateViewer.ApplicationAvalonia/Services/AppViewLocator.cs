using Avalonia;
using FFBitrateViewer.ApplicationAvalonia.ViewModels;
using FFBitrateViewer.ApplicationAvalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;

namespace FFBitrateViewer.ApplicationAvalonia.Services;

public class AppViewLocator : IViewLocator
{
    private readonly IServiceProvider? _serviceProvider;

    public AppViewLocator()
    {
        if (App.Current is FFBitrateViewer.ApplicationAvalonia.App app)
        {
            _serviceProvider = app.ServiceProvider;
        }
    }

    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
    {
        return viewModel switch
        {
            BitRateViewModel _ => GetViewForDataContext<BitRateView, T>(viewModel),
            AboutViewModel _ => GetViewForDataContext<AboutView, T>(viewModel),
            _ => throw new AppViewLocatorException()
        };
    }

    public TStyledElement GetViewForDataContext<TStyledElement, TDataContext>(TDataContext? dataContext)
        where TStyledElement : StyledElement, new()
    {
        if (_serviceProvider is not null)
        {
            var view = _serviceProvider.GetService<TStyledElement>()!;
            view.DataContext = dataContext;
            return view;
        }
        return new TStyledElement() { DataContext = dataContext };
    }
}

[Serializable]
public class AppViewLocatorException : FFBitrateViewerException
{
    public AppViewLocatorException() { }
    public AppViewLocatorException(string message) : base(message) { }
    public AppViewLocatorException(string message, Exception inner) : base(message, inner) { }

}