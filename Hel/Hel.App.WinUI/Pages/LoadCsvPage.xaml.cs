using System;
using Hel.App.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Hel.App.WinUI.Pages;

public sealed partial class LoadCsvPage : Page
{
    public LoadCsvPageViewModel ViewModel { get; }

    public LoadCsvPage(LoadCsvPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += LoadCsvPage_Loaded;
    }

    private async void LoadCsvPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= LoadCsvPage_Loaded;
        await ViewModel.InitializeAsync();
    }

    private async void SelectCsvButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".csv");

        nint hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        await ViewModel.LoadCsvAsync(file.Path);
    }
}
