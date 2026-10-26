using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using DynamicTopBar.Core.States;
using Xunit;

namespace DynamicTopBar.Tests;

public class FeatureNavigatorTests
{
    private static readonly FeatureDescriptor MediaFeature = new()
    {
        Id = "media",
        DisplayName = "Media Player",
        IconKey = "Music",
        DisplayOrder = 1
    };

    private static readonly FeatureDescriptor FocusFeature = new()
    {
        Id = "focus",
        DisplayName = "Focus Timer",
        IconKey = "Timer",
        DisplayOrder = 2
    };

    private static readonly FeatureDescriptor QuickLaunchFeature = new()
    {
        Id = "quick-launch",
        DisplayName = "Quick Launch",
        IconKey = "Rocket",
        DisplayOrder = 3
    };

    private static readonly FeatureDescriptor SystemGlanceFeature = new()
    {
        Id = "system-glance",
        DisplayName = "System Glance",
        IconKey = "System",
        DisplayOrder = 4
    };

    [Fact]
    public void ZeroModules_HasExpectedDefaults()
    {
        var navigator = new FeatureNavigator();

        Assert.Null(navigator.CurrentFeature);
        Assert.Equal(-1, navigator.CurrentIndex);
        Assert.Equal(0, navigator.TotalFeatures);
        Assert.False(navigator.CanNavigatePrevious);
        Assert.False(navigator.CanNavigateNext);
        Assert.Empty(navigator.EnabledFeatures);
        Assert.False(navigator.NavigatePrevious());
        Assert.False(navigator.NavigateNext());
    }

    [Fact]
    public void OneModule_DisablesPreviousAndNext()
    {
        var navigator = new FeatureNavigator(new[] { MediaFeature });

        Assert.NotNull(navigator.CurrentFeature);
        Assert.Equal("media", navigator.CurrentFeature.Id);
        Assert.Equal(0, navigator.CurrentIndex);
        Assert.Equal(1, navigator.TotalFeatures);
        Assert.False(navigator.CanNavigatePrevious);
        Assert.False(navigator.CanNavigateNext);

        Assert.False(navigator.NavigatePrevious());
        Assert.False(navigator.NavigateNext());
    }

    [Fact]
    public void MultipleModules_NavigatesSequentially()
    {
        var navigator = new FeatureNavigator(new[] { MediaFeature, FocusFeature, QuickLaunchFeature, SystemGlanceFeature });

        Assert.Equal(4, navigator.TotalFeatures);
        Assert.Equal("media", navigator.CurrentFeature?.Id);
        Assert.Equal(0, navigator.CurrentIndex);
        Assert.False(navigator.CanNavigatePrevious);
        Assert.True(navigator.CanNavigateNext);

        // Navigate forward
        Assert.True(navigator.NavigateNext());
        Assert.Equal("focus", navigator.CurrentFeature?.Id);
        Assert.Equal(1, navigator.CurrentIndex);
        Assert.True(navigator.CanNavigatePrevious);
        Assert.True(navigator.CanNavigateNext);

        Assert.True(navigator.NavigateNext());
        Assert.Equal("quick-launch", navigator.CurrentFeature?.Id);
        Assert.Equal(2, navigator.CurrentIndex);

        Assert.True(navigator.NavigateNext());
        Assert.Equal("system-glance", navigator.CurrentFeature?.Id);
        Assert.Equal(3, navigator.CurrentIndex);
        Assert.True(navigator.CanNavigatePrevious);
        Assert.False(navigator.CanNavigateNext);

        // Navigate backward
        Assert.True(navigator.NavigatePrevious());
        Assert.Equal("quick-launch", navigator.CurrentFeature?.Id);
        Assert.Equal(2, navigator.CurrentIndex);
    }

    [Fact]
    public void BoundaryNavigation_DoesNotWrapAround()
    {
        var navigator = new FeatureNavigator(new[] { MediaFeature, FocusFeature });

        // At first item
        Assert.False(navigator.CanNavigatePrevious);
        Assert.False(navigator.NavigatePrevious());
        Assert.Equal(0, navigator.CurrentIndex);

        // Move to last item
        Assert.True(navigator.NavigateNext());
        Assert.Equal(1, navigator.CurrentIndex);

        // At last item
        Assert.False(navigator.CanNavigateNext);
        Assert.False(navigator.NavigateNext());
        Assert.Equal(1, navigator.CurrentIndex);
    }

    [Fact]
    public void DisabledModules_AreFilteredFromNavigation()
    {
        var disabledFocus = FocusFeature with { IsEnabled = false };
        var navigator = new FeatureNavigator(new[] { MediaFeature, disabledFocus, QuickLaunchFeature });

        Assert.Equal(2, navigator.TotalFeatures);
        Assert.Equal("media", navigator.CurrentFeature?.Id);

        // Next should skip directly to quick-launch
        Assert.True(navigator.NavigateNext());
        Assert.Equal("quick-launch", navigator.CurrentFeature?.Id);
        Assert.Equal(1, navigator.CurrentIndex);
    }

    [Fact]
    public void NavigateTo_ValidAndInvalidId_HandledCorrectly()
    {
        var navigator = new FeatureNavigator(new[] { MediaFeature, FocusFeature, QuickLaunchFeature });

        // Valid direct navigation
        Assert.True(navigator.NavigateTo("quick-launch"));
        Assert.Equal("quick-launch", navigator.CurrentFeature?.Id);
        Assert.Equal(2, navigator.CurrentIndex);

        // Already at target returns true without error
        Assert.True(navigator.NavigateTo("quick-launch"));
        Assert.Equal(2, navigator.CurrentIndex);

        // Invalid ID returns false and preserves current selection
        Assert.False(navigator.NavigateTo("nonexistent-feature"));
        Assert.Equal("quick-launch", navigator.CurrentFeature?.Id);
        Assert.Equal(2, navigator.CurrentIndex);

        // Null or whitespace returns false
        Assert.False(navigator.NavigateTo(""));
        Assert.False(navigator.NavigateTo(null!));
    }

    [Fact]
    public void UnregisterFeature_UpdatesSelectionCleanly()
    {
        var navigator = new FeatureNavigator(new[] { MediaFeature, FocusFeature });

        Assert.True(navigator.NavigateTo("focus"));
        Assert.Equal("focus", navigator.CurrentFeature?.Id);

        // Unregister currently selected feature
        Assert.True(navigator.UnregisterFeature("focus"));
        Assert.Equal(1, navigator.TotalFeatures);
        Assert.Equal("media", navigator.CurrentFeature?.Id);
        Assert.Equal(0, navigator.CurrentIndex);
    }

    [Fact]
    public void CurrentFeatureChanged_FiresWithAccurateEventData()
    {
        var navigator = new FeatureNavigator(new[] { MediaFeature, FocusFeature });
        var events = new List<dynamic>();

        navigator.CurrentFeatureChanged += (s, e) => events.Add(e);

        navigator.NavigateNext();

        Assert.Single(events);
        Assert.Equal("media", events[0].PreviousFeature?.Id);
        Assert.Equal("focus", events[0].CurrentFeature?.Id);
        Assert.Equal(1, events[0].CurrentIndex);
        Assert.Equal(2, events[0].TotalFeatures);
    }

    [Fact]
    public void NavigationState_IsIndependentOfOverlayLifecycleState()
    {
        var timer = new TestTimerService();
        using var stateMachine = new OverlayStateMachine(null, timer);
        var navigator = new FeatureNavigator(new[] { MediaFeature, FocusFeature, QuickLaunchFeature });

        // Initial states
        Assert.Equal(OverlayState.Idle, stateMachine.CurrentState);
        Assert.Equal("media", navigator.CurrentFeature?.Id);

        // Change feature while overlay is Idle
        Assert.True(navigator.NavigateNext());
        Assert.Equal("focus", navigator.CurrentFeature?.Id);
        Assert.Equal(OverlayState.Idle, stateMachine.CurrentState); // Overlay state untouched!

        // Hover expand overlay
        stateMachine.PointerEntered();
        timer.LastTimer?.Trigger();
        Assert.Equal(OverlayState.Expanded, stateMachine.CurrentState);

        // Change feature while expanded
        Assert.True(navigator.NavigateNext());
        Assert.Equal("quick-launch", navigator.CurrentFeature?.Id);
        Assert.Equal(OverlayState.Expanded, stateMachine.CurrentState); // Overlay still expanded!
    }

    [Fact]
    public void FeatureNavigator_WithDefaultMultifunctionRegistry_LoadsAll11Features()
    {
        var registry = FeatureRegistry.CreateDefaultMultifunctionRegistry();
        var navigator = new FeatureNavigator(registry);

        Assert.Equal(11, navigator.TotalFeatures);
        Assert.Equal("media", navigator.CurrentFeature?.Id);
        Assert.Equal(0, navigator.CurrentIndex);
        Assert.False(navigator.CanNavigatePrevious);
        Assert.True(navigator.CanNavigateNext);

        // Navigate forward through all 11 features sequentially
        string[] expectedOrder = new[]
        {
            "media", "spotify", "clock", "weather", "todo",
            "focus", "quick-launch", "system-glance", "calendar", "notes", "clipboard"
        };

        for (int i = 0; i < expectedOrder.Length; i++)
        {
            Assert.Equal(expectedOrder[i], navigator.CurrentFeature?.Id);
            Assert.Equal(i, navigator.CurrentIndex);

            if (i < expectedOrder.Length - 1)
            {
                Assert.True(navigator.CanNavigateNext);
                Assert.True(navigator.NavigateNext());
            }
            else
            {
                // Boundary at last feature
                Assert.False(navigator.CanNavigateNext);
                Assert.False(navigator.NavigateNext()); // Does not wrap
            }
        }

        // Navigate backward all the way to beginning
        for (int i = expectedOrder.Length - 1; i >= 0; i--)
        {
            Assert.Equal(expectedOrder[i], navigator.CurrentFeature?.Id);
            Assert.Equal(i, navigator.CurrentIndex);

            if (i > 0)
            {
                Assert.True(navigator.CanNavigatePrevious);
                Assert.True(navigator.NavigatePrevious());
            }
            else
            {
                // Boundary at first feature
                Assert.False(navigator.CanNavigatePrevious);
                Assert.False(navigator.NavigatePrevious()); // Does not wrap
            }
        }
    }

    [Fact]
    public void FeatureNavigator_SkipsDisabledFeatures_AndMaintainsAccuratePageCount()
    {
        var registry = FeatureRegistry.CreateDefaultMultifunctionRegistry();
        var navigator = new FeatureNavigator(registry);

        // Disable all placeholders, keeping only the 5 real tools
        var prefs = new FeaturePreferences
        {
            EnabledFeatureIds = new() { "media", "clock", "focus", "quick-launch", "system-glance" },
            FeatureOrder = new() { "media", "clock", "focus", "quick-launch", "system-glance" },
            LastActiveFeatureId = "media"
        };

        navigator.ApplyPreferences(prefs);

        Assert.Equal(5, navigator.TotalFeatures);
        Assert.Equal("media", navigator.CurrentFeature?.Id);

        Assert.True(navigator.NavigateNext());
        Assert.Equal("clock", navigator.CurrentFeature?.Id); // skipped spotify!

        Assert.True(navigator.NavigateNext());
        Assert.Equal("focus", navigator.CurrentFeature?.Id); // skipped weather & todo!

        Assert.True(navigator.NavigateNext());
        Assert.Equal("quick-launch", navigator.CurrentFeature?.Id);

        Assert.True(navigator.NavigateNext());
        Assert.Equal("system-glance", navigator.CurrentFeature?.Id);

        Assert.False(navigator.CanNavigateNext); // End of list
    }

    [Fact]
    public void FeatureNavigator_PreservesActiveSelection_WhenUnrelatedFeatureDisabled()
    {
        var registry = FeatureRegistry.CreateDefaultMultifunctionRegistry();
        var navigator = new FeatureNavigator(registry, initialFeatureId: "focus");

        Assert.Equal("focus", navigator.CurrentFeature?.Id);

        // Disable "spotify" and "weather"
        var prefs = new FeaturePreferences
        {
            EnabledFeatureIds = new() { "media", "clock", "todo", "focus", "quick-launch", "system-glance" },
            FeatureOrder = new() { "media", "clock", "todo", "focus", "quick-launch", "system-glance" },
            LastActiveFeatureId = "focus"
        };

        navigator.ApplyPreferences(prefs);

        // Selection is maintained on "focus"!
        Assert.Equal("focus", navigator.CurrentFeature?.Id);
    }
}
