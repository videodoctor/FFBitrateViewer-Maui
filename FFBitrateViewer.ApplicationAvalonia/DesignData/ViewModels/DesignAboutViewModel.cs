using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.ViewModels;
using System.Collections.ObjectModel;

namespace FFBitrateViewer.ApplicationAvalonia.DesignData.ViewModels;

public partial class DesignAboutViewModel : ViewModelBase
{
    [RelayCommand]
    private void GoBack() { }

    [RelayCommand]
    private void OnLoaded() { }

    public ObservableCollection<ThirdPartyPackage> ThirdPartyPackages { get; set; } =
    [
        new ThirdPartyPackage("System.CommandLine.NamingConventionBinder", "2.0.0-beta4.22272.1", "https://github.com/dotnet/command-line-api", "\u00A9 Microsoft Corporation. All rights reserved.", "Microsoft", "MIT", "https://licenses.nuget.org/MIT", 0)
    ];
}