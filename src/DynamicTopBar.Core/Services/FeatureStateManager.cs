using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Interfaces;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Thread-safe in-memory state manager that preserves independent feature state across navigation.
/// </summary>
public sealed class FeatureStateManager : IFeatureStateManager
{
    private readonly object _lock = new();
    private readonly Dictionary<string, object> _states = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<string>? StateChanged;

    public void SetState<T>(string featureId, T state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureId);

        lock (_lock)
        {
            if (state == null)
            {
                _states.Remove(featureId);
            }
            else
            {
                _states[featureId] = state;
            }
        }

        StateChanged?.Invoke(this, featureId);
    }

    public T? GetState<T>(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId)) return default;

        lock (_lock)
        {
            if (_states.TryGetValue(featureId, out var val) && val is T typed)
            {
                return typed;
            }
        }

        return default;
    }

    public bool TryGetState<T>(string featureId, out T? state)
    {
        state = default;
        if (string.IsNullOrWhiteSpace(featureId)) return false;

        lock (_lock)
        {
            if (_states.TryGetValue(featureId, out var val) && val is T typed)
            {
                state = typed;
                return true;
            }
        }

        return false;
    }

    public bool HasState(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId)) return false;

        lock (_lock)
        {
            return _states.ContainsKey(featureId);
        }
    }

    public bool RemoveState(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId)) return false;

        bool removed;
        lock (_lock)
        {
            removed = _states.Remove(featureId);
        }

        if (removed)
        {
            StateChanged?.Invoke(this, featureId);
        }

        return removed;
    }

    public void ClearAll()
    {
        lock (_lock)
        {
            _states.Clear();
        }
    }
}
