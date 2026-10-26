using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Service coordinating local scratchpad notes, search, pin/unpin, and persistence.
/// </summary>
public sealed class NotesService : INotesService
{
    private readonly INotesRepository _repository;
    private readonly object _lock = new();
    private readonly List<NoteItem> _notes = new();
    private NoteItem? _selectedNote;
    private bool _isDisposed;

    public IReadOnlyList<NoteItem> Notes
    {
        get
        {
            lock (_lock)
            {
                return _notes
                    .OrderByDescending(n => n.IsPinned)
                    .ThenByDescending(n => n.ModifiedAt)
                    .ToList();
            }
        }
    }

    public NoteItem? SelectedNote
    {
        get { lock (_lock) return _selectedNote; }
        private set
        {
            lock (_lock) _selectedNote = value;
            SelectedNoteChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<IReadOnlyList<NoteItem>>? NotesChanged;
    public event EventHandler<NoteItem?>? SelectedNoteChanged;

    public NotesService(INotesRepository? repository = null)
    {
        _repository = repository ?? new FileNotesRepository();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed) return;
        var loaded = await _repository.GetAllAsync(cancellationToken);
        lock (_lock)
        {
            _notes.Clear();
            _notes.AddRange(loaded);
            _selectedNote = _notes.OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.ModifiedAt).FirstOrDefault();
        }

        NotifyChanged();
        SelectedNoteChanged?.Invoke(this, _selectedNote);
    }

    public async Task<NoteItem> CreateNoteAsync(string? title = null, string? content = null, CancellationToken cancellationToken = default)
    {
        string noteTitle = string.IsNullOrWhiteSpace(title) ? "New Note" : title.Trim();
        string noteContent = content ?? string.Empty;

        var note = new NoteItem
        {
            Id = Guid.NewGuid(),
            Title = noteTitle,
            Content = noteContent,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            IsPinned = false
        };

        lock (_lock)
        {
            _notes.Insert(0, note);
            _selectedNote = note;
        }

        await SaveAsync(cancellationToken);
        NotifyChanged();
        SelectedNoteChanged?.Invoke(this, note);
        return note;
    }

    public async Task<bool> UpdateNoteAsync(Guid id, string title, string content, CancellationToken cancellationToken = default)
    {
        NoteItem? updated = null;
        lock (_lock)
        {
            int idx = _notes.FindIndex(n => n.Id == id);
            if (idx >= 0)
            {
                var existing = _notes[idx];
                updated = existing with
                {
                    Title = string.IsNullOrWhiteSpace(title) ? "Untitled Note" : title.Trim(),
                    Content = content ?? string.Empty,
                    ModifiedAt = DateTimeOffset.UtcNow
                };
                _notes[idx] = updated;

                if (_selectedNote?.Id == id)
                {
                    _selectedNote = updated;
                }
            }
        }

        if (updated != null)
        {
            await SaveAsync(cancellationToken);
            NotifyChanged();
            return true;
        }

        return false;
    }

    public async Task<bool> TogglePinAsync(Guid id, CancellationToken cancellationToken = default)
    {
        bool found = false;
        lock (_lock)
        {
            int idx = _notes.FindIndex(n => n.Id == id);
            if (idx >= 0)
            {
                var existing = _notes[idx];
                _notes[idx] = existing with { IsPinned = !existing.IsPinned };
                found = true;
            }
        }

        if (found)
        {
            await SaveAsync(cancellationToken);
            NotifyChanged();
        }

        return found;
    }

    public async Task<bool> DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        bool removed;
        lock (_lock)
        {
            removed = _notes.RemoveAll(n => n.Id == id) > 0;
            if (removed && _selectedNote?.Id == id)
            {
                _selectedNote = _notes.OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.ModifiedAt).FirstOrDefault();
            }
        }

        if (removed)
        {
            await SaveAsync(cancellationToken);
            NotifyChanged();
            SelectedNoteChanged?.Invoke(this, _selectedNote);
        }

        return removed;
    }

    public void SelectNote(Guid? id)
    {
        if (id == null)
        {
            SelectedNote = null;
            return;
        }

        lock (_lock)
        {
            SelectedNote = _notes.FirstOrDefault(n => n.Id == id.Value);
        }
    }

    public IReadOnlyList<NoteItem> SearchNotes(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Notes;
        }

        string q = query.Trim();
        lock (_lock)
        {
            return _notes
                .Where(n => n.Title.Contains(q, StringComparison.OrdinalIgnoreCase) || n.Content.Contains(q, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.ModifiedAt)
                .ToList();
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        List<NoteItem> snapshot;
        lock (_lock)
        {
            snapshot = _notes.ToList();
        }
        await _repository.SaveAllAsync(snapshot, cancellationToken);
    }

    private void NotifyChanged()
    {
        NotesChanged?.Invoke(this, Notes);
    }

    public void Dispose()
    {
        _isDisposed = true;
    }
}
