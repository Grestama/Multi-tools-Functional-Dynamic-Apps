using DynamicTopBar.Core.Interfaces;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Passthrough implementation of IDataProtector for non-Windows environments, testing, or disabled encryption.
/// </summary>
public sealed class NoOpDataProtector : IDataProtector
{
    public static readonly NoOpDataProtector Instance = new();

    public string Protect(string plainText) => plainText;

    public string Unprotect(string cipherText) => cipherText;
}
