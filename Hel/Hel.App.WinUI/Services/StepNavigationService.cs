using System;
using System.Threading.Tasks;
using Hel.App.WinUI.Pages;
using Hel.Application.Wizard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Hel.App.WinUI.Services;

/// <summary>
/// Owns the shell Frame and creates step pages through DI.
/// </summary>
public sealed class StepNavigationService : IStepNavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WizardState _wizardState;

    private Frame? _frame;

    public event EventHandler<StepId>? CurrentStepChanged;

    public StepId? CurrentStep { get; private set; }

    public StepNavigationService(IServiceProvider serviceProvider, WizardState wizardState)
    {
        _serviceProvider = serviceProvider;
        _wizardState = wizardState;
    }

    public void AttachFrame(Frame frame)
    {
        _frame = frame;
    }

    public Task<bool> NavigateToAsync(StepId step)
    {
        if (_frame is null)
            throw new InvalidOperationException("Shell Frame has not been attached.");

        if (!_wizardState.IsStepEnabled(step))
            return Task.FromResult(false);

        Page page = step switch
        {
            StepId.Start => _serviceProvider.GetRequiredService<StartPage>(),
            StepId.LoadCsv => _serviceProvider.GetRequiredService<LoadCsvPage>(),
            StepId.SelectLocations => _serviceProvider.GetRequiredService<SelectLocationsPage>(),
            StepId.PreviewResults => _serviceProvider.GetRequiredService<PreviewResultsPage>(),
            StepId.ExportFinish => _serviceProvider.GetRequiredService<ExportFinishPage>(),
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Unsupported step.")
        };

        _frame.Content = page;
        CurrentStep = step;
        CurrentStepChanged?.Invoke(this, step);

        return Task.FromResult(true);
    }
}
