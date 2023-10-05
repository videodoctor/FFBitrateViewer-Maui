using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System;

namespace FFBitrateViewer.ApplicationAvalonia.Services
{

    public class AppProcessService
    {
        internal static IClassicDesktopStyleApplicationLifetime? DesktopApplication => _desktopApplication.Value;

        private static readonly Lazy<IClassicDesktopStyleApplicationLifetime?> _desktopApplication = new(() => Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);

        public void ExecutionOnUIThread(Action action)
            => Dispatcher.UIThread.Post(action, default);
        

        public void Exit(int exitCode = 0)
        {
            _desktopApplication.Value?.Shutdown(exitCode);
        }

    }

}