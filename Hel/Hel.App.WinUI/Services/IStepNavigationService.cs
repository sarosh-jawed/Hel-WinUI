using System;
using System.Threading.Tasks;
using Hel.Application.Wizard;
using Microsoft.UI.Xaml.Controls;

namespace Hel.App.WinUI.Services;

/// <summary>
/// Navigation service for the Phase 9 stepper shell.
/// </summary>
public interface IStepNavigationService
{
    event EventHandler<StepId>? CurrentStepChanged;

    StepId? CurrentStep { get; }

    void AttachFrame(Frame frame);

    Task<bool> NavigateToAsync(StepId step);
}
