using System;
using System.Diagnostics;
using DynamicTopBar.Core.Interfaces;

namespace DynamicTopBar.App.Platform;

/// <summary>
/// Concrete Windows platform adapter implementing IProcessLauncher via Process.Start.
/// </summary>
public sealed class WindowsProcessLauncher : IProcessLauncher
{
    public bool Launch(string fileName, string? arguments = null)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                UseShellExecute = true
            };

            Process.Start(psi);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
