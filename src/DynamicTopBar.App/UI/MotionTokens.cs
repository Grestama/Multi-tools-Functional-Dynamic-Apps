using System;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;

namespace DynamicTopBar.App.UI;

/// <summary>
/// Centralized motion tokens defining durations, easing curves, and accessibility checks
/// for fluid Apple Dynamic Island-inspired transitions in DynamicTopBar.
/// </summary>
public static class MotionTokens
{
    private static readonly UISettings? _uiSettings;

    static MotionTokens()
    {
        try
        {
            _uiSettings = new UISettings();
        }
        catch
        {
            _uiSettings = null;
        }
    }

    /// <summary>
    /// Checks whether system animations are enabled in Windows Settings (Ease of Access / Accessibility).
    /// </summary>
    public static bool AreAnimationsEnabled
    {
        get
        {
            try
            {
                return _uiSettings?.AnimationsEnabled ?? true;
            }
            catch
            {
                return true;
            }
        }
    }

    // --- Standard Motion Durations ---

    /// <summary>
    /// Micro interactions (hover highlight, press feedback, indicator shifts): 100ms
    /// </summary>
    public static TimeSpan MicroDuration => Adjust(TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Fast transitions (cross-fade, status badge pop, ticker swap): 160ms
    /// </summary>
    public static TimeSpan FastDuration => Adjust(TimeSpan.FromMilliseconds(160));

    /// <summary>
    /// Normal transitions (pill morphing, module switching, equalizer bounce): 220ms
    /// </summary>
    public static TimeSpan NormalDuration => Adjust(TimeSpan.FromMilliseconds(220));

    /// <summary>
    /// Expressive transitions (full panel expand / collapse, slide reveal): 260ms
    /// </summary>
    public static TimeSpan ExpressiveDuration => Adjust(TimeSpan.FromMilliseconds(260));

    /// <summary>
    /// Returns 1ms if animations are disabled by Windows accessibility settings; otherwise returns the original duration.
    /// </summary>
    public static TimeSpan Adjust(TimeSpan duration)
    {
        return AreAnimationsEnabled ? duration : TimeSpan.FromMilliseconds(1);
    }

    // --- Standard Easing Functions ---

    /// <summary>
    /// Standard deceleration curve for elements entering the screen or expanding outwards.
    /// </summary>
    public static CubicEase EaseOutCubic => new() { EasingMode = EasingMode.EaseOut };

    /// <summary>
    /// Standard acceleration curve for elements collapsing or exiting the screen.
    /// </summary>
    public static CubicEase EaseInCubic => new() { EasingMode = EasingMode.EaseIn };

    /// <summary>
    /// Smooth symmetric curve for shape morphing between idle and expanded dimensions.
    /// </summary>
    public static CubicEase EaseInOutCubic => new() { EasingMode = EasingMode.EaseInOut };
}
