namespace DynamicTopBar.Tests;

using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.States;
using Xunit;

public class OverlayStateTests
{
    [Fact]
    public void OverlayConfig_HasExpectedDefaultBaselineValues()
    {
        var config = new OverlayConfig();

        Assert.Equal(44.0, config.IdleHeight);
        Assert.Equal(340.0, config.IdleWidth);
        Assert.Equal(224.0, config.ExpandedHeight);
        Assert.Equal(376.0, config.ExpandedWidth);
        Assert.Equal(22.0, config.CapsuleCornerRadius);
        Assert.Equal(54.0, config.TriggerBarWidth);
        Assert.Equal(4.0, config.TriggerBarHeight);
        Assert.Equal(500, config.HoverDelayMs);
        Assert.Equal(4000, config.CollapseDelayMs);
    }

    [Fact]
    public void FeaturePreferences_HasAutoHideEnabledByDefault()
    {
        var prefs = new FeaturePreferences();
        Assert.True(prefs.AutoHideEnabled);
    }

    [Fact]
    public void OverlayState_DefinesAllRequiredUXStates()
    {
        var states = Enum.GetValues<OverlayState>();

        Assert.Contains(OverlayState.Idle, states);
        Assert.Contains(OverlayState.HoverPending, states);
        Assert.Contains(OverlayState.Expanded, states);
        Assert.Contains(OverlayState.CollapsePending, states);
        Assert.Equal(4, states.Length);
    }
}
