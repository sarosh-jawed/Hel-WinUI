using System;
using System.Threading.Tasks;
using Hel.App.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Hel.App.WinUI.Pages;

public sealed partial class ExportFinishPage : Page
{
    public ExportFinishPageViewModel ViewModel { get; }

    public ExportFinishPage(ExportFinishPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += ExportFinishPage_Loaded;
    }

    private async void ExportFinishPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= ExportFinishPage_Loaded;
        await ViewModel.InitializeAsync();
    }

    public Task GenerateReportsAsync()
    {
        return ViewModel.GenerateReportsAsync();
    }

    private async void ChooseOutputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        nint hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is null)
            return;

        ViewModel.SetOutputFolder(folder.Path);
    }

    private void OpenOutputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenOutputFolder();
    }

    private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenLogFolder();
    }
}
