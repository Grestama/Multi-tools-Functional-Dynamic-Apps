using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Service managing dynamic command registration, intelligent query scoring, and action execution.
/// Decoupled from WinUI presentation for testability and extensibility.
/// </summary>
public sealed class CommandPaletteService : ICommandPaletteService
{
    private readonly object _lock = new();
    private readonly Dictionary<string, PaletteCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private bool _isOpen;
    private bool _isDisposed;

    public IReadOnlyList<PaletteCommand> Commands
    {
        get
        {
            lock (_lock)
            {
                return _commands.Values.Where(c => c.IsAvailable).ToList();
            }
        }
    }

    public bool IsOpen
    {
        get { lock (_lock) return _isOpen; }
        set
        {
            lock (_lock)
            {
                if (_isOpen == value) return;
                _isOpen = value;
            }
            VisibilityChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool>? VisibilityChanged;
    public event EventHandler<string>? CommandExecuted;
    public event EventHandler<PaletteCommand>? CommandFailed;

    public CommandPaletteService(IEnumerable<PaletteCommand>? initialCommands = null)
    {
        if (initialCommands != null)
        {
            foreach (var cmd in initialCommands)
            {
                Register(cmd);
            }
        }
    }

    public void Register(PaletteCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.Id))
        {
            throw new ArgumentException("Command ID cannot be null or whitespace.", nameof(command));
        }

        lock (_lock)
        {
            _commands[command.Id] = command;
        }
    }

    public bool Unregister(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId)) return false;

        lock (_lock)
        {
            return _commands.Remove(commandId);
        }
    }

    public IReadOnlyList<PaletteCommand> Search(string? query, string? activeModuleId = null)
    {
        List<PaletteCommand> available;
        lock (_lock)
        {
            available = _commands.Values.Where(c => c.IsAvailable).ToList();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            // Empty query: prioritize commands relevant to the currently active module
            return available
                .OrderByDescending(c => IsModuleMatch(c, activeModuleId))
                .ThenBy(c => c.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        string cleanQuery = query.Trim();
        var scored = new List<(PaletteCommand Command, int Score)>();

        foreach (var cmd in available)
        {
            int score = CalculateMatchScore(cmd, cleanQuery, activeModuleId);
            if (score > 0)
            {
                scored.Add((cmd, score));
            }
        }

        return scored
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Command.Title, StringComparer.OrdinalIgnoreCase)
            .Select(s => s.Command)
            .ToList();
    }

    public async Task<bool> ExecuteCommandAsync(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId)) return false;

        PaletteCommand? command;
        lock (_lock)
        {
            _commands.TryGetValue(commandId, out command);
        }

        if (command == null || !command.IsAvailable)
        {
            return false;
        }

        try
        {
            bool ok = await command.ExecuteAsync();
            if (ok)
            {
                CommandExecuted?.Invoke(this, commandId);
            }
            else
            {
                CommandFailed?.Invoke(this, command);
            }
            return ok;
        }
        catch
        {
            CommandFailed?.Invoke(this, command);
            return false;
        }
    }

    private static int CalculateMatchScore(PaletteCommand cmd, string query, string? activeModuleId)
    {
        int score = 0;

        // Title matches
        if (cmd.Title.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 150;
        }
        else if (cmd.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 100;
        }
        else if (cmd.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 60;
        }

        // Category match
        if (cmd.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 40;
        }

        // Keywords match
        if (cmd.Keywords.Any(k => k.Contains(query, StringComparison.OrdinalIgnoreCase)))
        {
            score += 35;
        }

        // Description match
        if (!string.IsNullOrEmpty(cmd.Description) && cmd.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 15;
        }

        // Active module contextual bonus
        if (score > 0 && IsModuleMatch(cmd, activeModuleId))
        {
            score += 20;
        }

        return score;
    }

    private static bool IsModuleMatch(PaletteCommand cmd, string? activeModuleId)
    {
        if (string.IsNullOrWhiteSpace(activeModuleId)) return false;

        return cmd.Category.Contains(activeModuleId, StringComparison.OrdinalIgnoreCase) ||
               cmd.Id.Contains(activeModuleId, StringComparison.OrdinalIgnoreCase) ||
               cmd.Keywords.Any(k => k.Equals(activeModuleId, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
