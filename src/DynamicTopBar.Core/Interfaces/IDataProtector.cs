namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service abstraction for protecting (encrypting) and unprotecting (decrypting) sensitive local text data.
/// Implementations must support backward-compatible fallback for unencrypted plaintext.
/// </summary>
public interface IDataProtector
{
    /// <summary>
    /// Encrypts or protects the given plain text string.
    /// </summary>
    string Protect(string plainText);

    /// <summary>
    /// Decrypts or unprotects the given ciphertext string.
    /// If the text was stored as unencrypted plaintext (legacy), returns it as-is without throwing.
    /// </summary>
    string Unprotect(string cipherText);
}
