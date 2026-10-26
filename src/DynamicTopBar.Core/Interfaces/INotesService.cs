using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing scratchpad notes, search, pin/unpin, and autosave.
/// </summary>
public interface INotesService : IDisposable
{
    IReadOnlyList<NoteItem> Notes { get; }
    NoteItem? SelectedNote { get; }

    event EventHandler<IReadOnlyList<NoteItem>>? NotesChanged;
    event EventHandler<NoteItem?>? SelectedNoteChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<NoteItem> CreateNoteAsync(string? title = null, string? content = null, CancellationToken cancellationToken = default);
    Task<bool> UpdateNoteAsync(Guid id, string title, string content, CancellationToken cancellationToken = default);
    Task<bool> TogglePinAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default);
    void SelectNote(Guid? id);
    IReadOnlyList<NoteItem> SearchNotes(string query);
}
