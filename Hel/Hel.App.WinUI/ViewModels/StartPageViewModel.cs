namespace Hel.App.WinUI.ViewModels;

/// <summary>
/// View model for the Start step.
/// </summary>
public sealed class StartPageViewModel : ObservableObject
{
    public string WelcomeTitle => "Hel";

    public string WelcomeText =>
        "Use the guided workflow on the left to load a Monthly Missing Items CSV, review routing results, and generate recipient-ready text files.";
}
