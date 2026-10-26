using System;
using System.Security.Cryptography;
using System.Text;
using DynamicTopBar.Core.Interfaces;

namespace DynamicTopBar.App.Platform;

/// <summary>
/// Production DPAPI-backed data protector for Windows desktop.
/// Encrypts local sensitive payloads (e.g. Quick Notes, Clipboard history) to the current user profile.
/// Fully backward-compatible with unencrypted legacy JSON payloads.
/// </summary>
public sealed class WindowsDpapiDataProtector : IDataProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DynamicTopBar_LocalData_Entropy_v1");
    public const string EncryptedPrefix = "DPAPI:";

    public string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
            return EncryptedPrefix + Convert.ToBase64String(encryptedBytes);
        }
        catch
        {
            // Graceful fallback: return unencrypted plainText if DPAPI fails
            return plainText;
        }
    }

    public string Unprotect(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return cipherText;
        }

        // Backward compatibility: If the text is plain JSON (starts with { or [) or does not have DPAPI prefix, return as-is.
        if (!cipherText.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            return cipherText;
        }

        try
        {
            string base64 = cipherText.Substring(EncryptedPrefix.Length);
            byte[] encryptedBytes = Convert.FromBase64String(base64);
            byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            // Gracefully recover on corrupt ciphertext
            return cipherText;
        }
    }
}
