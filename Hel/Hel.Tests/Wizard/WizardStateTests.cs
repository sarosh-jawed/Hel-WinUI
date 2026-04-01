using Hel.Application.Wizard;
using Xunit;

namespace Hel.Tests.Wizard;

public class WizardStateTests
{
    [Fact]
    public void Initialize_Should_Set_Expected_Default_State()
    {
        var state = new WizardState();

        Assert.Equal(StepId.Start, state.CurrentStep);
        Assert.Equal(StepState.Available, state.GetStepState(StepId.Start));
        Assert.Equal(StepState.Available, state.GetStepState(StepId.LoadCsv));
        Assert.Equal(StepState.Locked, state.GetStepState(StepId.SelectLocations));
        Assert.Equal(StepState.Locked, state.GetStepState(StepId.PreviewResults));
        Assert.Equal(StepState.Locked, state.GetStepState(StepId.ExportFinish));
    }

    [Fact]
    public void MarkCsvLoaded_Should_Complete_LoadCsv_And_Unlock_SelectLocations()
    {
        var state = new WizardState();

        state.MarkStartVisited();
        state.MarkCsvLoaded();

        Assert.True(state.HasCsvLoaded);
        Assert.Equal(StepState.Completed, state.GetStepState(StepId.LoadCsv));
        Assert.Equal(StepState.Available, state.GetStepState(StepId.SelectLocations));
        Assert.Equal(StepState.Locked, state.GetStepState(StepId.PreviewResults));
        Assert.Equal(StepState.Locked, state.GetStepState(StepId.ExportFinish));
    }

    [Fact]
    public void UpdateSelectedLocationsCount_Should_Unlock_Preview_When_Count_Is_Positive()
    {
        var state = new WizardState();

        state.MarkStartVisited();
        state.MarkCsvLoaded();
        state.UpdateSelectedLocationsCount(3);

        Assert.Equal(StepState.Completed, state.GetStepState(StepId.SelectLocations));
        Assert.Equal(StepState.Available, state.GetStepState(StepId.PreviewResults));
        Assert.Equal(StepState.Locked, state.GetStepState(StepId.ExportFinish));
    }

    [Fact]
    public void MarkPreviewReady_Should_Unlock_Export()
    {
        var state = new WizardState();

        state.MarkStartVisited();
        state.MarkCsvLoaded();
        state.UpdateSelectedLocationsCount(2);
        state.MarkPreviewReady();

        Assert.True(state.HasPreview);
        Assert.Equal(StepState.Completed, state.GetStepState(StepId.PreviewResults));
        Assert.Equal(StepState.Available, state.GetStepState(StepId.ExportFinish));
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

        Assert.False(state.HasPreview);
        Assert.Equal(StepState.Available, state.GetStepState(StepId.PreviewResults));
        Assert.Equal(StepState.Locked, state.GetStepState(StepId.ExportFinish));
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

        Assert.Equal(StepState.Available, state.GetStepState(StepId.SelectLocations));
        Assert.Equal(StepState.Locked, state.GetStepState(StepId.PreviewResults));
        Assert.Equal(StepState.Locked, state.GetStepState(StepId.ExportFinish));
    }
}
