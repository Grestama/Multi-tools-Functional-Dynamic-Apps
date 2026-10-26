using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing To-do items, completion toggling, filtering, and CRUD operations.
/// </summary>
public interface ITodoService : IDisposable
{
    IReadOnlyList<TodoItem> Items { get; }
    int ActiveCount { get; }
    int CompletedCount { get; }

    event EventHandler<IReadOnlyList<TodoItem>>? ItemsChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<TodoItem> AddItemAsync(string title, TodoPriority priority = TodoPriority.Normal, DateTimeOffset? dueDate = null, string? notes = null, CancellationToken cancellationToken = default);
    Task UpdateItemAsync(TodoItem item, CancellationToken cancellationToken = default);
    Task<bool> ToggleCompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task ClearCompletedAsync(CancellationToken cancellationToken = default);
}
