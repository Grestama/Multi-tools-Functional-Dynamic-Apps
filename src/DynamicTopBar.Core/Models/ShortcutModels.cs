using System;
using System.Text.Json.Serialization;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Bitwise key modifiers matching Win32 MOD_* flags.
/// </summary>
[Flags]
public enum ShortcutModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}

/// <summary>
/// Standard virtual key codes for configurable hotkeys.
/// </summary>
public enum ShortcutVirtualKey
{
    None = 0,
    A = 0x41, B = 0x42, C = 0x43, D = 0x44, E = 0x45, F = 0x46, G = 0x47,
    H = 0x48, I = 0x49, J = 0x4A, K = 0x4B, L = 0x4C, M = 0x4D, N = 0x4E,
    O = 0x4F, P = 0x50, Q = 0x51, R = 0x52, S = 0x53, T = 0x54, U = 0x55,
    V = 0x56, W = 0x57, X = 0x58, Y = 0x59, Z = 0x5A,
    D0 = 0x30, D1 = 0x31, D2 = 0x32, D3 = 0x33, D4 = 0x34,
    D5 = 0x35, D6 = 0x36, D7 = 0x37, D8 = 0x38, D9 = 0x39,
    F1 = 0x70, F2 = 0x71, F3 = 0x72, F4 = 0x73, F5 = 0x74, F6 = 0x75,
    F7 = 0x76, F8 = 0x77, F9 = 0x78, F10 = 0x79, F11 = 0x7A, F12 = 0x7B,
    Space = 0x20, Tab = 0x09, Escape = 0x1B, Return = 0x0D,
    Left = 0x25, Up = 0x26, Right = 0x27, Down = 0x28,
    OemTilde = 0xC0 // `~` key
}

/// <summary>
/// Represents a configurable global shortcut bound to an action.
/// </summary>
public record GlobalShortcut
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ActionId { get; init; } = string.Empty;
    public ShortcutModifiers Modifiers { get; init; } = ShortcutModifiers.Control | ShortcutModifiers.Shift;
    public ShortcutVirtualKey Key { get; init; } = ShortcutVirtualKey.P;
    public bool IsEnabled { get; init; } = true;
    public bool IsCustom { get; init; }

    [JsonIgnore]
    public string FormattedKeyCombo
    {
        get
        {
            var parts = new System.Collections.Generic.List<string>();
            if (Modifiers.HasFlag(ShortcutModifiers.Control)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(ShortcutModifiers.Alt)) parts.Add("Alt");
            if (Modifiers.HasFlag(ShortcutModifiers.Shift)) parts.Add("Shift");
            if (Modifiers.HasFlag(ShortcutModifiers.Windows)) parts.Add("Win");
            parts.Add(Key.ToString());
            return string.Join(" + ", parts);
        }
    }
}

public record ShortcutValidationResult(bool IsValid, string? ErrorMessage = null)
{
    public static ShortcutValidationResult Success => new(true);
    public static ShortcutValidationResult Fail(string error) => new(false, error);
}

public record ShortcutRegistrationResult(string ShortcutId, bool IsRegistered, string? ErrorMessage = null);
