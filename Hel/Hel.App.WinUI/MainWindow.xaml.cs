using System;
using System.IO;
using Hel.App.WinUI.Pages;
using Hel.App.WinUI.Services;
using Hel.App.WinUI.ViewModels;
using Hel.Application.Wizard;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;

namespace Hel.App.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly IStepNavigationService _stepNavigationService;
    private readonly WizardSessionStore _session;
    private bool _isUpdatingNavigationSelection;
    private bool _isInitialized;

    public static MainWindow? Instance { get; private set; }

    public ShellViewModel ViewModel { get; }

    public string BusyMessageText => ViewModel.IsBusy ? _session.BusyMessage : string.Empty;

    public MainWindow(
        ShellViewModel viewModel,
        IStepNavigationService stepNavigationService,
        WizardSessionStore session)
    {
        ViewModel = viewModel;
        _stepNavigationService = stepNavigationService;
        _session = session;

        InitializeComponent();

        Instance = this;

        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new SizeInt32(1380, 940));

        TryApplyWindowIcon(appWindow);
        TryApplyMicaBackdrop();

        TryApplyMicaBackdrop();

        _stepNavigationService.AttachFrame(ShellFrame);
        ViewModel.StateChanged += ViewModel_StateChanged;
        Activated += MainWindow_Activated;
    }

    private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        Activated -= MainWindow_Activated;

        await ViewModel.InitializeAsync();
        RenderNavigationItems();
        Bindings.Update();
    }

    private void ViewModel_StateChanged(object? sender, EventArgs e)
    {
        RenderNavigationItems();
        Bindings.Update();
    }

    private async void ShellNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_isUpdatingNavigationSelection)
            return;

        if (args.SelectedItem is NavigationViewItem item &&
            item.Tag is StepId step)
        {
            await ViewModel.NavigateToAsync(step);
        }
    }

    private async void BackButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.GoBackAsync();
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentStep == StepId.ExportFinish &&
            ShellFrame.Content is ExportFinishPage exportPage)
        {
            await exportPage.GenerateReportsAsync();
            return;
        }

        await ViewModel.GoNextAsync();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelCurrentOperation();
        Bindings.Update();
    }

    private void RenderNavigationItems()
    {
        _isUpdatingNavigationSelection = true;

        try
        {
            ShellNavigationView.MenuItems.Clear();

            NavigationViewItem? activeItem = null;

            foreach (var step in ViewModel.StepItems)
            {
                IconElement icon = step.IsCompleted
                    ? new SymbolIcon(Symbol.Accept)
                    : new FontIcon { Glyph = step.Glyph };

                var item = new NavigationViewItem
                {
                    Content = step.Title,
                    Tag = step.StepId,
                    Icon = icon,
                    IsEnabled = step.IsEnabled
                };

                if (step.IsActive)
                {
                    item.Foreground = (Brush)Microsoft.UI.Xaml.Application.Current.Resources["SidebarActiveForegroundBrush"];
                }
                else if (step.IsCompleted)
                {
                    item.Foreground = (Brush)Microsoft.UI.Xaml.Application.Current.Resources["CompletedStepBrush"];
                }
                else if (!step.IsEnabled)
                {
                    item.Foreground = (Brush)Microsoft.UI.Xaml.Application.Current.Resources["SidebarMutedForegroundBrush"];
                }
                else
                {
                    item.Foreground = (Brush)Microsoft.UI.Xaml.Application.Current.Resources["SidebarForegroundBrush"];
                }

                ShellNavigationView.MenuItems.Add(item);

                if (step.IsActive)
                    activeItem = item;
            }

            ShellNavigationView.SelectedItem = activeItem;
        }
        finally
        {
            _isUpdatingNavigationSelection = false;
        }
    }

    private void TryApplyMicaBackdrop()
    {
        try
        {
            SystemBackdrop = new MicaBackdrop
            {
                Kind = MicaKind.BaseAlt
            };
        }
        catch
        {
            // Fallback to solid shell brushes when Mica is unavailable.
        }
    }

    private void TryApplyWindowIcon(AppWindow appWindow)
    {
        try
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "HelApp.ico");

            if (File.Exists(iconPath))
            {
                appWindow.SetIcon(iconPath);
            }
        }
        catch
        {
            // Fallback to default icon if the custom icon is unavailable.
        }
    }
}
