using Hel.App.WinUI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Hel.App.WinUI.Pages;

public sealed partial class StartPage : Page
{
    public StartPageViewModel ViewModel { get; }

    public StartPage(StartPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
