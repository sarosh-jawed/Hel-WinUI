using FluentAssertions;
using Hel.Application.Wizard;
using Xunit;

namespace Hel.Tests.Wizard;

public class WizardStateTests
{
    [Fact]
    public void Initialize_Should_Set_Expected_Default_State()
    {
        var state = new WizardState();

        state.CurrentStep.Should().Be(StepId.Start);
        state.GetStepState(StepId.Start).Should().Be(StepState.Available);
        state.GetStepState(StepId.LoadCsv).Should().Be(StepState.Available);
        state.GetStepState(StepId.SelectLocations).Should().Be(StepState.Locked);
        state.GetStepState(StepId.PreviewResults).Should().Be(StepState.Locked);
        state.GetStepState(StepId.ExportFinish).Should().Be(StepState.Locked);
    }

    [Fact]
    public void MarkCsvLoaded_Should_Complete_LoadCsv_And_Unlock_SelectLocations()
    {
        var state = new WizardState();

        state.MarkStartVisited();
        state.MarkCsvLoaded();

        state.HasCsvLoaded.Should().BeTrue();
        state.GetStepState(StepId.LoadCsv).Should().Be(StepState.Completed);
        state.GetStepState(StepId.SelectLocations).Should().Be(StepState.Available);
        state.GetStepState(StepId.PreviewResults).Should().Be(StepState.Locked);
        state.GetStepState(StepId.ExportFinish).Should().Be(StepState.Locked);
    }

    [Fact]
    public void UpdateSelectedLocationsCount_Should_Unlock_Preview_When_Count_Is_Positive()
    {
        var state = new WizardState();

        state.MarkStartVisited();
        state.MarkCsvLoaded();
        state.UpdateSelectedLocationsCount(3);

        state.GetStepState(StepId.SelectLocations).Should().Be(StepState.Completed);
        state.GetStepState(StepId.PreviewResults).Should().Be(StepState.Available);
        state.GetStepState(StepId.ExportFinish).Should().Be(StepState.Locked);
    }

    [Fact]
    public void MarkPreviewReady_Should_Unlock_Export()
    {
        var state = new WizardState();

        state.MarkStartVisited();
        state.MarkCsvLoaded();
        state.UpdateSelectedLocationsCount(2);
        state.MarkPreviewReady();

        state.HasPreview.Should().BeTrue();
        state.GetStepState(StepId.PreviewResults).Should().Be(StepState.Completed);
        state.GetStepState(StepId.ExportFinish).Should().Be(StepState.Available);
    }

    [Fact]
    public void ResetAfterLocationChange_Should_Clear_Preview_And_Lock_Export()
    {
        var state = new WizardState();

        state.MarkStartVisited();
        state.MarkCsvLoaded();
        state.UpdateSelectedLocationsCount(2);
        state.MarkPreviewReady();
        state.ResetAfterLocationChange();

        state.HasPreview.Should().BeFalse();
        state.GetStepState(StepId.PreviewResults).Should().Be(StepState.Available);
        state.GetStepState(StepId.ExportFinish).Should().Be(StepState.Locked);
    }

    [Fact]
    public void UpdateSelectedLocationsCount_WithZero_Should_Lock_Preview_And_Export()
    {
        var state = new WizardState();

        state.MarkStartVisited();
        state.MarkCsvLoaded();
        state.UpdateSelectedLocationsCount(1);
        state.MarkPreviewReady();
        state.UpdateSelectedLocationsCount(0);

        state.GetStepState(StepId.SelectLocations).Should().Be(StepState.Available);
        state.GetStepState(StepId.PreviewResults).Should().Be(StepState.Locked);
        state.GetStepState(StepId.ExportFinish).Should().Be(StepState.Locked);
    }
}
