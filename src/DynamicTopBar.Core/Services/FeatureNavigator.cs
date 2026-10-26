using System;
using System.Collections.Generic;
using System.Linq;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Platform-independent navigator managing feature registration, ordering,
/// and non-wrapping boundary navigation for the top bar container.
/// </summary>
public sealed class FeatureNavigator : IFeatureNavigator
{
    private readonly object _lock = new();
    private readonly Dictionary<string, FeatureDescriptor> _features = new(StringComparer.OrdinalIgnoreCase);

    private List<FeatureDescriptor> _enabledFeatures = new();
    private int _currentIndex = -1;

    public event EventHandler<FeatureNavigationChangedEventArgs>? CurrentFeatureChanged;

    public FeatureDescriptor? CurrentFeature
    {
        get
        {
            lock (_lock)
            {
                return _currentIndex >= 0 && _currentIndex < _enabledFeatures.Count
                    ? _enabledFeatures[_currentIndex]
                    : null;
            }
        }
    }

    public int CurrentIndex
    {
        get
        {
            lock (_lock)
            {
                return _currentIndex;
            }
        }
    }

    public int TotalFeatures
    {
        get
        {
            lock (_lock)
            {
                return _enabledFeatures.Count;
            }
        }
    }

    public bool CanNavigatePrevious
    {
        get
        {
            lock (_lock)
            {
                return _currentIndex > 0;
            }
        }
    }

    public bool CanNavigateNext
    {
        get
        {
            lock (_lock)
            {
                return _currentIndex >= 0 && _currentIndex < _enabledFeatures.Count - 1;
            }
        }
    }

    public IReadOnlyList<FeatureDescriptor> EnabledFeatures
    {
        get
        {
            lock (_lock)
            {
                return _enabledFeatures.ToList();
            }
        }
    }

    public FeatureNavigator(IFeatureRegistry registry, string? initialFeatureId = null)
        : this(registry?.GetAllDescriptors(), initialFeatureId)
    {
        if (registry != null)
        {
            registry.FeatureRegistered += (s, desc) => RegisterFeature(desc);
            registry.FeatureUnregistered += (s, id) => UnregisterFeature(id);
        }
    }

    public FeatureNavigator(IEnumerable<FeatureDescriptor>? initialFeatures = null, string? initialFeatureId = null)
    {
        if (initialFeatures != null)
        {
            foreach (var feature in initialFeatures)
            {
                _features[feature.Id] = feature;
            }
        }

        RebuildEnabledList(initialFeatureId);
    }

    public bool NavigatePrevious()
    {
        FeatureNavigationChangedEventArgs? changeArgs = null;
        lock (_lock)
        {
            if (_currentIndex <= 0)
            {
                return false;
            }

            var previous = _enabledFeatures[_currentIndex];
            _currentIndex--;
            var current = _enabledFeatures[_currentIndex];

            changeArgs = new FeatureNavigationChangedEventArgs(previous, current, _currentIndex, _enabledFeatures.Count);
        }

        CurrentFeatureChanged?.Invoke(this, changeArgs);
        return true;
    }

    public bool NavigateNext()
    {
        FeatureNavigationChangedEventArgs? changeArgs = null;
        lock (_lock)
        {
            if (_currentIndex < 0 || _currentIndex >= _enabledFeatures.Count - 1)
            {
                return false;
            }

            var previous = _enabledFeatures[_currentIndex];
            _currentIndex++;
            var current = _enabledFeatures[_currentIndex];

            changeArgs = new FeatureNavigationChangedEventArgs(previous, current, _currentIndex, _enabledFeatures.Count);
        }

        CurrentFeatureChanged?.Invoke(this, changeArgs);
        return true;
    }

    public bool NavigateTo(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId))
        {
            return false;
        }

        FeatureNavigationChangedEventArgs? changeArgs = null;
        lock (_lock)
        {
            int targetIndex = _enabledFeatures.FindIndex(f => string.Equals(f.Id, featureId, StringComparison.OrdinalIgnoreCase));
            if (targetIndex < 0)
            {
                return false;
            }

            if (targetIndex == _currentIndex)
            {
                return true; // Already selected
            }

            var previous = _currentIndex >= 0 && _currentIndex < _enabledFeatures.Count ? _enabledFeatures[_currentIndex] : null;
            _currentIndex = targetIndex;
            var current = _enabledFeatures[_currentIndex];

            changeArgs = new FeatureNavigationChangedEventArgs(previous, current, _currentIndex, _enabledFeatures.Count);
        }

        CurrentFeatureChanged?.Invoke(this, changeArgs);
        return true;
    }

    public void RegisterFeature(FeatureDescriptor feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        FeatureNavigationChangedEventArgs? changeArgs = null;
        lock (_lock)
        {
            string? currentId = CurrentFeature?.Id;
            _features[feature.Id] = feature;
            changeArgs = RebuildEnabledList(currentId);
        }

        if (changeArgs != null)
        {
            CurrentFeatureChanged?.Invoke(this, changeArgs);
        }
    }

    public bool UnregisterFeature(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId))
        {
            return false;
        }

        FeatureNavigationChangedEventArgs? changeArgs = null;
        bool removed;
        lock (_lock)
        {
            string? currentId = CurrentFeature?.Id;
            removed = _features.Remove(featureId);
            if (removed)
            {
                changeArgs = RebuildEnabledList(currentId);
            }
        }

        if (changeArgs != null)
        {
            CurrentFeatureChanged?.Invoke(this, changeArgs);
        }

        return removed;
    }

    public void ApplyPreferences(FeaturePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        FeatureNavigationChangedEventArgs? changeArgs = null;
        lock (_lock)
        {
            var enabledSet = new HashSet<string>(preferences.EnabledFeatureIds, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in _features.ToList())
            {
                var feature = kvp.Value;
                bool isEnabled = enabledSet.Contains(feature.Id);
                int orderIndex = preferences.FeatureOrder.FindIndex(id => string.Equals(id, feature.Id, StringComparison.OrdinalIgnoreCase));
                int newOrder = orderIndex >= 0 ? orderIndex + 1 : feature.DisplayOrder;

                _features[kvp.Key] = feature with
                {
                    IsEnabled = isEnabled,
                    DisplayOrder = newOrder
                };
            }

            changeArgs = RebuildEnabledList(preferences.LastActiveFeatureId ?? CurrentFeature?.Id);
        }

        if (changeArgs != null)
        {
            CurrentFeatureChanged?.Invoke(this, changeArgs);
        }
    }

    private FeatureNavigationChangedEventArgs? RebuildEnabledList(string? preferredSelectedId)
    {
        var previous = _currentIndex >= 0 && _currentIndex < _enabledFeatures.Count
            ? _enabledFeatures[_currentIndex]
            : null;

        _enabledFeatures = _features.Values
            .Where(f => f.IsEnabled)
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (_enabledFeatures.Count == 0)
        {
            _currentIndex = -1;
            if (previous != null)
            {
                return new FeatureNavigationChangedEventArgs(previous, null, -1, 0);
            }
            return null;
        }

        // Try to restore preferred ID
        int newIndex = -1;
        if (!string.IsNullOrWhiteSpace(preferredSelectedId))
        {
            newIndex = _enabledFeatures.FindIndex(f => string.Equals(f.Id, preferredSelectedId, StringComparison.OrdinalIgnoreCase));
        }

        // Fallback to first available enabled feature if previous/preferred ID not found
        if (newIndex < 0)
        {
            newIndex = 0;
        }

        _currentIndex = newIndex;
        var current = _enabledFeatures[_currentIndex];

        if (!ReferenceEquals(previous, current))
        {
            return new FeatureNavigationChangedEventArgs(previous, current, _currentIndex, _enabledFeatures.Count);
        }

        return null;
    }
}
