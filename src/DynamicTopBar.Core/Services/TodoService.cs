using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Domain service implementing To-do list operations, priority sorting, and persistence.
/// </summary>
public sealed class TodoService : ITodoService
{
    private readonly ITodoRepository _repository;
    private readonly object _lock = new();
    private readonly List<TodoItem> _items = new();
    private bool _isDisposed;

    public IReadOnlyList<TodoItem> Items
    {
        get
        {
            lock (_lock)
            {
                return _items.OrderBy(i => i.IsCompleted)
                             .ThenByDescending(i => (int)i.Priority)
                             .ThenBy(i => i.DueDate ?? DateTimeOffset.MaxValue)
                             .ThenByDescending(i => i.CreatedAt)
                             .ToList();
            }
        }
    }

    public int ActiveCount
    {
        get
        {
            lock (_lock)
            {
                return _items.Count(i => !i.IsCompleted);
            }
        }
    }

    public int CompletedCount
    {
        get
        {
            lock (_lock)
            {
                return _items.Count(i => i.IsCompleted);
            }
        }
    }

    public event EventHandler<IReadOnlyList<TodoItem>>? ItemsChanged;

    public TodoService(ITodoRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var loaded = await _repository.GetAllAsync(cancellationToken);
        lock (_lock)
        {
            _items.Clear();
            _items.AddRange(loaded);
        }

        NotifyChanged();
    }

    public async Task<TodoItem> AddItemAsync(string title, TodoPriority priority = TodoPriority.Normal, DateTimeOffset? dueDate = null, string? notes = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title cannot be empty.", nameof(title));
        }

        var item = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Priority = priority,
            DueDate = dueDate,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            IsCompleted = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        lock (_lock)
        {
            _items.Add(item);
        }

        await PersistAsync(cancellationToken);
        NotifyChanged();
        return item;
    }

    public async Task UpdateItemAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        bool found = false;
        lock (_lock)
        {
            int index = _items.FindIndex(i => i.Id == item.Id);
            if (index >= 0)
            {
                _items[index] = item;
                found = true;
            }
        }

        if (found)
        {
            await PersistAsync(cancellationToken);
            NotifyChanged();
        }
    }

    public async Task<bool> ToggleCompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        bool toggled = false;
        lock (_lock)
        {
            int index = _items.FindIndex(i => i.Id == id);
            if (index >= 0)
            {
                var current = _items[index];
                bool newCompleted = !current.IsCompleted;
                _items[index] = current with
                {
                    IsCompleted = newCompleted,
                    CompletedAt = newCompleted ? DateTimeOffset.UtcNow : null
                };
                toggled = true;
            }
        }

        if (toggled)
        {
            await PersistAsync(cancellationToken);
            NotifyChanged();
        }

        return toggled;
    }

    public async Task<bool> DeleteItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        bool removed = false;
        lock (_lock)
        {
            int index = _items.FindIndex(i => i.Id == id);
            if (index >= 0)
            {
                _items.RemoveAt(index);
                removed = true;
            }
        }

        if (removed)
        {
            await PersistAsync(cancellationToken);
            NotifyChanged();
        }

        return removed;
    }

    public async Task ClearCompletedAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _items.RemoveAll(i => i.IsCompleted);
        }

        await PersistAsync(cancellationToken);
        NotifyChanged();
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        List<TodoItem> snapshot;
        lock (_lock)
        {
            snapshot = new List<TodoItem>(_items);
        }

        await _repository.SaveAllAsync(snapshot, cancellationToken);
    }

    private void NotifyChanged()
    {
        ItemsChanged?.Invoke(this, Items);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
