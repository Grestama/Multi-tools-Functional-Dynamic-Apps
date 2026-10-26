using System;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Hub service coordinating composite productivity views and cross-module workflows
/// across Calendar, Todo, Notes, and Focus Timer.
/// </summary>
public interface IProductivityHubService : IDisposable
{
    ProductivityGlance CurrentGlance { get; }

    event EventHandler<ProductivityGlance>? GlanceChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    ProductivityGlance RefreshGlance();
    Task<TodoItem> QuickAddTodoAsync(string title, TodoPriority priority = TodoPriority.Normal, CancellationToken cancellationToken = default);
    Task<NoteItem> QuickAddNoteAsync(string title, string content, CancellationToken cancellationToken = default);
    void QuickStartFocusTimer();
}
