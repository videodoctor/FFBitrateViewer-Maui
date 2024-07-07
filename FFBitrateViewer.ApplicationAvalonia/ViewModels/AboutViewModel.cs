using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public partial class AboutViewModel(IScreen screen) : RoutableViewModelBase(screen, nameof(AboutViewModel))
{
    public ObservableCollection<ThirdPartyPackage> ThirdPartyPackages { get; set; } = [];

    [RelayCommand]
    private void GoBack()
    {
        if (HostScreen.Router.NavigationStack.Count == 0)
        { return; }

        HostScreen.Router.NavigateBack.Execute();
    }

    [RelayCommand]
    private void OnLoaded()
    {
        var packages = JsonSerializer.Deserialize<List<ThirdPartyPackage>>(File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Content{Path.DirectorySeparatorChar}nuget-license.json")));

        if (packages is null)
        { return; }

        packages.ForEach(ThirdPartyPackages.Add);
    }

}

public record ThirdPartyPackage(
    string? PackageId,
    string? PackageVersion,
    string? PackageProjectUrl,
    string? Copyright,
    string? Authors,
    string? License,
    string? LicenseUrl,
    int? LicenseInformationOrigin
);