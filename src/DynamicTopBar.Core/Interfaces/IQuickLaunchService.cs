using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing pinned application, folder, and URL shortcuts.
/// </summary>
public interface IQuickLaunchService
{
    /// <summary>
    /// Current list of active quick launch items.
    /// </summary>
    IReadOnlyList<QuickLaunchItem> Items { get; }

    /// <summary>
    /// Raised whenever quick launch items are added, removed, or updated.
    /// </summary>
    event EventHandler? ItemsChanged;

    /// <summary>
    /// Validates whether a target path or URI is safe and well-formed.
    /// </summary>
    QuickLaunchResult ValidateTarget(string pathOrUri, QuickLaunchTargetType type);

    /// <summary>
    /// Safely launches the item matching the specified ID.
    /// </summary>
    QuickLaunchResult Launch(string itemId);

    /// <summary>
    /// Adds a valid shortcut item.
    /// </summary>
    QuickLaunchResult AddItem(QuickLaunchItem item);

    /// <summary>
    /// Recent items launched through Quick Launch.
    /// </summary>
    IReadOnlyList<QuickLaunchItem> RecentItems { get; }

    /// <summary>
    /// Clears the recent launch history.
    /// </summary>
    void ClearRecentHistory();

    /// <summary>
    /// Removes a custom shortcut item by ID.
    /// </summary>
    bool RemoveItem(string itemId);
}
