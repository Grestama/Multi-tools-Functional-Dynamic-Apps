using System;
using System.Security.Cryptography;
using System.Text;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Helper class for OAuth 2.0 Proof Key for Code Exchange (PKCE) according to RFC 7636.
/// </summary>
public static class SpotifyPkceAuthHelper
{
    private const string UnreservedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._~";

    /// <summary>
    /// Generates a cryptographically random code verifier between 43 and 128 characters.
    /// </summary>
    public static string GenerateCodeVerifier(int length = 64)
    {
        if (length < 43 || length > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "PKCE code_verifier length must be between 43 and 128 characters.");
        }

        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);

        var sb = new StringBuilder(length);
        foreach (var b in bytes)
        {
            sb.Append(UnreservedCharacters[b % UnreservedCharacters.Length]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Computes the Base64Url-encoded SHA-256 hash of the code verifier (code_challenge).
    /// </summary>
    public static string GenerateCodeChallenge(string codeVerifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeVerifier);

        byte[] challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(challengeBytes);
    }

    /// <summary>
    /// Converts a byte array to Base64Url string (without padding '=' and replacing '+' and '/').
    /// </summary>
    public static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Builds the standard Spotify authorization URL with PKCE parameters.
    /// </summary>
    public static string BuildAuthorizeUrl(string clientId, string redirectUri, string codeChallenge, string scope, string state)
    {
        var query = new StringBuilder("https://accounts.spotify.com/authorize?");
        query.Append("client_id=").Append(Uri.EscapeDataString(clientId));
        query.Append("&response_type=code");
        query.Append("&redirect_uri=").Append(Uri.EscapeDataString(redirectUri));
        query.Append("&code_challenge_method=S256");
        query.Append("&code_challenge=").Append(Uri.EscapeDataString(codeChallenge));
        query.Append("&scope=").Append(Uri.EscapeDataString(scope));
        query.Append("&state=").Append(Uri.EscapeDataString(state));

        return query.ToString();
    }

    /// <summary>
    /// Builds the standard Spotify authorization URL with PKCE parameters using an enumerable of scopes.
    /// </summary>
    public static string BuildAuthorizeUrl(string clientId, string redirectUri, string codeChallenge, string state, System.Collections.Generic.IEnumerable<string> scopes)
    {
        return BuildAuthorizeUrl(clientId, redirectUri, codeChallenge, string.Join(" ", scopes), state);
    }
}
