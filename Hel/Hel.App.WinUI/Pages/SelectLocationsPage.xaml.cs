using Hel.App.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Hel.App.WinUI.Pages;

public sealed partial class SelectLocationsPage : Page
{
    public SelectLocationsPageViewModel ViewModel { get; }

    public SelectLocationsPage(SelectLocationsPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += SelectLocationsPage_Loaded;
    }

    private async void SelectLocationsPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= SelectLocationsPage_Loaded;
        await ViewModel.InitializeAsync();
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAll();
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearAll();
    }
}
