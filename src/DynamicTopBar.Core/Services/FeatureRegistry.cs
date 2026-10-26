using System;
using System.Collections.Generic;
using System.Linq;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Thread-safe registry maintaining registered feature modules and ensuring ID uniqueness.
/// </summary>
public sealed class FeatureRegistry : IFeatureRegistry
{
    private readonly object _lock = new();
    private readonly Dictionary<string, FeatureDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IFeatureModule> _modules = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<FeatureDescriptor>? FeatureRegistered;
    public event EventHandler<string>? FeatureUnregistered;

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _descriptors.Count;
            }
        }
    }

    public FeatureRegistry()
    {
    }

    public FeatureRegistry(IEnumerable<FeatureDescriptor> initialDescriptors)
    {
        if (initialDescriptors != null)
        {
            foreach (var desc in initialDescriptors)
            {
                Register(desc);
            }
        }
    }

    public void Register(IFeatureModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        var descriptor = module.Descriptor ?? throw new ArgumentException("Module descriptor cannot be null.", nameof(module));

        lock (_lock)
        {
            if (_descriptors.ContainsKey(descriptor.Id))
            {
                throw new ArgumentException($"A feature with ID '{descriptor.Id}' is already registered.", nameof(module));
            }

            _descriptors[descriptor.Id] = descriptor;
            _modules[descriptor.Id] = module;
        }

        FeatureRegistered?.Invoke(this, descriptor);
    }

    public void Register(FeatureDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (string.IsNullOrWhiteSpace(descriptor.Id))
        {
            throw new ArgumentException("Feature ID cannot be null or whitespace.", nameof(descriptor));
        }

        lock (_lock)
        {
            if (_descriptors.ContainsKey(descriptor.Id))
            {
                throw new ArgumentException($"A feature with ID '{descriptor.Id}' is already registered.", nameof(descriptor));
            }

            _descriptors[descriptor.Id] = descriptor;
        }

        FeatureRegistered?.Invoke(this, descriptor);
    }

    public bool Unregister(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId)) return false;

        bool removed;
        lock (_lock)
        {
            removed = _descriptors.Remove(featureId);
            _modules.Remove(featureId);
        }

        if (removed)
        {
            FeatureUnregistered?.Invoke(this, featureId);
        }

        return removed;
    }

    public bool Contains(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId)) return false;

        lock (_lock)
        {
            return _descriptors.ContainsKey(featureId);
        }
    }

    public FeatureDescriptor? GetDescriptor(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId)) return null;

        lock (_lock)
        {
            return _descriptors.TryGetValue(featureId, out var descriptor) ? descriptor : null;
        }
    }

    public IFeatureModule? GetModule(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId)) return null;

        lock (_lock)
        {
            return _modules.TryGetValue(featureId, out var module) ? module : null;
        }
    }

    public IReadOnlyList<FeatureDescriptor> GetAllDescriptors()
    {
        lock (_lock)
        {
            return _descriptors.Values
                .OrderBy(d => d.DisplayOrder)
                .ThenBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public IReadOnlyList<IFeatureModule> GetAllModules()
    {
        lock (_lock)
        {
            return _modules.Values
                .OrderBy(m => m.Descriptor.DisplayOrder)
                .ThenBy(m => m.Descriptor.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    /// <summary>
    /// Creates the standard multifunction feature registry populated with the 11 planned feature definitions.
    /// </summary>
    public static FeatureRegistry CreateDefaultMultifunctionRegistry()
    {
        var registry = new FeatureRegistry();

        // 1. Media Player (Implemented)
        registry.Register(new FeatureDescriptor
        {
            Id = "media",
            DisplayName = "Media Player",
            IconKey = "\uE8D6",
            DisplayOrder = 1,
            RoadmapPhase = 2,
            IsPlaceholder = false,
            Description = "Realtime playback metadata, artwork, and media session controls"
        });

        // 2. Spotify (Implemented in Phase 2: Web API + PKCE OAuth)
        registry.Register(new FeatureDescriptor
        {
            Id = "spotify",
            DisplayName = "Spotify",
            IconKey = "\uE8D6",
            DisplayOrder = 2,
            RoadmapPhase = 2,
            IsPlaceholder = false,
            Description = "Spotify PKCE OAuth, user profile, currently playing, and Web API playback controls"
        });

        // 3. Clock & Date (Implemented with Windows Local Time)
        registry.Register(new FeatureDescriptor
        {
            Id = "clock",
            DisplayName = "Clock & Date",
            IconKey = "\uE823",
            DisplayOrder = 3,
            RoadmapPhase = 2,
            IsPlaceholder = false,
            Description = "Digital clock, current date, and Windows local timezone"
        });

        // 4. Weather (Implemented in Phase 2: Open-Meteo Geocoding & Forecast)
        registry.Register(new FeatureDescriptor
        {
            Id = "weather",
            DisplayName = "Weather",
            IconKey = "\uE706",
            DisplayOrder = 4,
            RoadmapPhase = 2,
            IsPlaceholder = false,
            Description = "City search, current conditions, precipitation, and 3-day forecast glance"
        });

        // 5. To-do List (Implemented in Phase 2: Task CRUD & JSON Persistence)
        registry.Register(new FeatureDescriptor
        {
            Id = "todo",
            DisplayName = "To-do List",
            IconKey = "\uE762",
            DisplayOrder = 5,
            RoadmapPhase = 2,
            IsPlaceholder = false,
            Description = "Task checklist, priorities, due dates, and persistent local storage"
        });

        // 6. Focus Timer (Implemented)
        registry.Register(new FeatureDescriptor
        {
            Id = "focus",
            DisplayName = "Focus Timer",
            IconKey = "\uE914",
            DisplayOrder = 6,
            RoadmapPhase = 2,
            IsPlaceholder = false,
            Description = "Pomodoro countdown timer with work/break presets and continuous background execution"
        });

        // 7. Quick Launcher (Implemented)
        registry.Register(new FeatureDescriptor
        {
            Id = "quick-launch",
            DisplayName = "Quick Launcher",
            IconKey = "\uE71D",
            DisplayOrder = 7,
            RoadmapPhase = 2,
            IsPlaceholder = false,
            Description = "Verified shortcuts to File Explorer, Browser, Terminal, Calculator, and Task Manager"
        });

        // 8. System Glance (Implemented)
        registry.Register(new FeatureDescriptor
        {
            Id = "system-glance",
            DisplayName = "System Glance",
            IconKey = "\uE770",
            DisplayOrder = 8,
            RoadmapPhase = 2,
            IsPlaceholder = false,
            Description = "Battery status, device identification, and system uptime"
        });

        // 9. Calendar (Implemented in Phase 3: Events & ICS import/export)
        registry.Register(new FeatureDescriptor
        {
            Id = "calendar",
            DisplayName = "Calendar",
            IconKey = "\uE787",
            DisplayOrder = 9,
            RoadmapPhase = 3,
            IsPlaceholder = false,
            Description = "Upcoming schedule, today's agenda, and iCalendar (.ics) import/export"
        });

        // 10. Quick Notes (Implemented in Phase 3: Scratchpad & Search)
        registry.Register(new FeatureDescriptor
        {
            Id = "notes",
            DisplayName = "Quick Notes",
            IconKey = "\uE70B",
            DisplayOrder = 10,
            RoadmapPhase = 3,
            IsPlaceholder = false,
            Description = "Fast-access persistent desktop scratchpad with search, pin, and autosave"
        });

        // 11. Clipboard (Implemented in Phase 3: Opt-in Privacy Snippet History)
        registry.Register(new FeatureDescriptor
        {
            Id = "clipboard",
            DisplayName = "Clipboard",
            IconKey = "\uE77F",
            DisplayOrder = 11,
            RoadmapPhase = 3,
            IsPlaceholder = false,
            Description = "Opt-in clipboard snippet history with one-click copy and zero external telemetry"
        });

        return registry;
    }
}
