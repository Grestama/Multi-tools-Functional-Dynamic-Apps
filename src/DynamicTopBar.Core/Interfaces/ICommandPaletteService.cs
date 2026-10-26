using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing searchable commands, fuzzy search scoring, dynamic actions, and execution.
/// </summary>
public interface ICommandPaletteService : IDisposable
{
    IReadOnlyList<PaletteCommand> Commands { get; }
    bool IsOpen { get; set; }

    event EventHandler<bool>? VisibilityChanged;
    event EventHandler<string>? CommandExecuted;
    event EventHandler<PaletteCommand>? CommandFailed;

    void Register(PaletteCommand command);
    void RegisterCommand(PaletteCommand command) => Register(command);
    bool Unregister(string commandId);
    IReadOnlyList<PaletteCommand> Search(string? query, string? activeModuleId = null);
    Task<bool> ExecuteCommandAsync(string commandId);
    Task<bool> ExecuteCommandAsync(PaletteCommand command) => ExecuteCommandAsync(command.Id);
}
