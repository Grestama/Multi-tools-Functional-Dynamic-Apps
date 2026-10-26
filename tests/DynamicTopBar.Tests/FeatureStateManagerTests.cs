using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class FeatureStateManagerTests
{
    private record SampleState(string Text, int Counter);

    [Fact]
    public void SetState_And_GetState_PreservesStateAcrossCalls()
    {
        var manager = new FeatureStateManager();
        var state = new SampleState("FocusSession", 25);

        manager.SetState("focus", state);

        Assert.True(manager.HasState("focus"));
        var retrieved = manager.GetState<SampleState>("focus");
        Assert.NotNull(retrieved);
        Assert.Equal("FocusSession", retrieved.Text);
        Assert.Equal(25, retrieved.Counter);
    }

    [Fact]
    public void IndependentState_BetweenFeatures_DoesNotCrossContaminate()
    {
        var manager = new FeatureStateManager();
        manager.SetState("focus", new SampleState("FocusActive", 1));
        manager.SetState("media", new SampleState("MediaPlaying", 99));

        var focusState = manager.GetState<SampleState>("focus");
        var mediaState = manager.GetState<SampleState>("media");

        Assert.NotNull(focusState);
        Assert.NotNull(mediaState);
        Assert.Equal("FocusActive", focusState.Text);
        Assert.Equal("MediaPlaying", mediaState.Text);
    }

    [Fact]
    public void GetState_WrongType_ReturnsDefault()
    {
        var manager = new FeatureStateManager();
        manager.SetState("test", "A plain string");

        var wrongType = manager.GetState<SampleState>("test");
        Assert.Null(wrongType);
    }

    [Fact]
    public void RemoveState_ClearsOnlySpecifiedFeature()
    {
        var manager = new FeatureStateManager();
        manager.SetState("f1", "val1");
        manager.SetState("f2", "val2");

        Assert.True(manager.RemoveState("f1"));
        Assert.False(manager.HasState("f1"));
        Assert.True(manager.HasState("f2"));
        Assert.Equal("val2", manager.GetState<string>("f2"));
    }

    [Fact]
    public void SetState_Null_RemovesState()
    {
        var manager = new FeatureStateManager();
        manager.SetState("f1", "active");
        Assert.True(manager.HasState("f1"));

        manager.SetState<string?>("f1", null);
        Assert.False(manager.HasState("f1"));
    }

    [Fact]
    public void ClearAll_RemovesAllStates()
    {
        var manager = new FeatureStateManager();
        manager.SetState("f1", 1);
        manager.SetState("f2", 2);

        manager.ClearAll();

        Assert.False(manager.HasState("f1"));
        Assert.False(manager.HasState("f2"));
    }
}
