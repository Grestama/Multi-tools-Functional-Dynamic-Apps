using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class MemoryNotesRepository : INotesRepository
{
    private List<NoteItem> _notes = new();

    public Task<IReadOnlyList<NoteItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<NoteItem>>(_notes.ToList());
    }

    public Task SaveAllAsync(IReadOnlyList<NoteItem> notes, CancellationToken cancellationToken = default)
    {
        _notes = notes.ToList();
        return Task.CompletedTask;
    }
}

public class NotesServiceTests
{
    [Fact]
    public async Task CreateNoteAsync_DefaultParameters_CreatesNoteWithDefaultTitleAndSelectsIt()
    {
        var repo = new MemoryNotesRepository();
        var service = new NotesService(repo);

        bool notesChanged = false;
        bool selectedChanged = false;
        service.NotesChanged += (s, e) => notesChanged = true;
        service.SelectedNoteChanged += (s, e) => selectedChanged = true;

        var note = await service.CreateNoteAsync();

        Assert.True(notesChanged);
        Assert.True(selectedChanged);
        Assert.Equal("New Note", note.Title);
        Assert.Equal(string.Empty, note.Content);
        Assert.False(note.IsPinned);
        Assert.NotNull(service.SelectedNote);
        Assert.Equal(note.Id, service.SelectedNote.Id);
        Assert.Single(service.Notes);
    }

    [Fact]
    public async Task UpdateNoteAsync_ModifiesContentAndUpdatesTimestamp()
    {
        var repo = new MemoryNotesRepository();
        var service = new NotesService(repo);

        var note = await service.CreateNoteAsync("Meeting Ideas", "1. Better UX");
        var originalModified = note.ModifiedAt;

        await Task.Delay(20);

        bool updated = await service.UpdateNoteAsync(note.Id, "Meeting Ideas 2026", "1. Better UX\n2. Faster load");

        Assert.True(updated);
        var refreshed = service.SelectedNote;
        Assert.NotNull(refreshed);
        Assert.Equal("Meeting Ideas 2026", refreshed.Title);
        Assert.Contains("2. Faster load", refreshed.Content);
        Assert.True(refreshed.ModifiedAt >= originalModified);
    }

    [Fact]
    public async Task TogglePinAsync_PinningNote_FloatsToTop()
    {
        var repo = new MemoryNotesRepository();
        var service = new NotesService(repo);

        var note1 = await service.CreateNoteAsync("Note One", "Content 1");
        await Task.Delay(10);
        var note2 = await service.CreateNoteAsync("Note Two", "Content 2");

        // Note 2 is newer so by default it's at index 0
        Assert.Equal(note2.Id, service.Notes[0].Id);

        // Pin Note 1
        bool pinned = await service.TogglePinAsync(note1.Id);
        Assert.True(pinned);

        // Now Note 1 must be at index 0 because it is pinned
        Assert.Equal(note1.Id, service.Notes[0].Id);
        Assert.True(service.Notes[0].IsPinned);
    }

    [Fact]
    public async Task SearchNotes_MatchesTitleAndContentCaseInsensitive()
    {
        var repo = new MemoryNotesRepository();
        var service = new NotesService(repo);

        await service.CreateNoteAsync("Project Alpha", "Budget calculations");
        await service.CreateNoteAsync("Shopping List", "Apples, milk, bread");
        await service.CreateNoteAsync("Urgent Bug", "Memory leak in background timer");

        var resultsTitle = service.SearchNotes("urgent");
        Assert.Single(resultsTitle);
        Assert.Equal("Urgent Bug", resultsTitle[0].Title);

        var resultsContent = service.SearchNotes("milk");
        Assert.Single(resultsContent);
        Assert.Equal("Shopping List", resultsContent[0].Title);
    }

    [Fact]
    public async Task DeleteNoteAsync_SelectedNote_RemovesAndSelectsNextAvailable()
    {
        var repo = new MemoryNotesRepository();
        var service = new NotesService(repo);

        var note1 = await service.CreateNoteAsync("Note 1", "Body 1");
        var note2 = await service.CreateNoteAsync("Note 2", "Body 2");

        Assert.Equal(note2.Id, service.SelectedNote?.Id);

        bool deleted = await service.DeleteNoteAsync(note2.Id);

        Assert.True(deleted);
        Assert.Single(service.Notes);
        Assert.Equal(note1.Id, service.SelectedNote?.Id);
    }

    [Fact]
    public async Task SelectNote_ValidId_UpdatesSelectedNote()
    {
        var repo = new MemoryNotesRepository();
        var service = new NotesService(repo);

        var note1 = await service.CreateNoteAsync("Note 1", "Body 1");
        var note2 = await service.CreateNoteAsync("Note 2", "Body 2");

        Assert.Equal(note2.Id, service.SelectedNote?.Id);

        service.SelectNote(note1.Id);
        Assert.Equal(note1.Id, service.SelectedNote?.Id);
    }
}
