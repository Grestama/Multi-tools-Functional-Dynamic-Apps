namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Abstraction for safely launching external processes, URLs, or shell commands.
/// </summary>
public interface IProcessLauncher
{
    /// <summary>
    /// Launches the specified file or URI with optional arguments.
    /// </summary>
    bool Launch(string fileName, string? arguments = null);
}
