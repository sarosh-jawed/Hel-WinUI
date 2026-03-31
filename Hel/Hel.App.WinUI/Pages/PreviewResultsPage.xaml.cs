using Hel.App.WinUI.Models;
using Hel.App.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Hel.App.WinUI.Pages;

public sealed partial class PreviewResultsPage : Page
{
    public PreviewResultsPageViewModel ViewModel { get; }

    public PreviewResultsPage(PreviewResultsPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += PreviewResultsPage_Loaded;
    }

    private async void PreviewResultsPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= PreviewResultsPage_Loaded;
        await ViewModel.InitializeAsync();
    }

    private void ShowAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PreviewBucketViewModel bucket)
        {
            bucket.ShowAll();
        }
    }

    private void ShowFirstButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PreviewBucketViewModel bucket)
        {
            bucket.ShowFirst(50);
        }
    }
}
