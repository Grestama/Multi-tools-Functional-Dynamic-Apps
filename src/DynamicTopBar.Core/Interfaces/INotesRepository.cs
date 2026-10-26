using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Persistence contract for local Quick Notes.
/// </summary>
public interface INotesRepository
{
    Task<IReadOnlyList<NoteItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAllAsync(IReadOnlyList<NoteItem> notes, CancellationToken cancellationToken = default);
}
