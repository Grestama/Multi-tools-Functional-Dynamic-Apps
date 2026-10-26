using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.App.Platform;

/// <summary>
/// Secure token storage implementation using Windows Data Protection API (DPAPI).
/// Encrypts credentials specifically for the current Windows user profile.
/// </summary>
public sealed class WindowsProtectedDataTokenStore : ISpotifyTokenStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DynamicTopBar_Spotify_Entropy_v1");
    private readonly string _filePath;

    public WindowsProtectedDataTokenStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar",
            "spotify_tokens.dat");
    }

    public Task<SpotifyToken?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return Task.FromResult<SpotifyToken?>(null);
        }

        try
        {
            byte[] encryptedBytes = File.ReadAllBytes(_filePath);
            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
            string json = Encoding.UTF8.GetString(decryptedBytes);
            var token = JsonSerializer.Deserialize<SpotifyToken>(json);
            return Task.FromResult(token);
        }
        catch
        {
            // If data is corrupt or cannot be unprojected, return null
            return Task.FromResult<SpotifyToken?>(null);
        }
    }

    public Task SaveTokenAsync(SpotifyToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        try
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(token);
            byte[] plainBytes = Encoding.UTF8.GetBytes(json);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(_filePath, encryptedBytes);
        }
        catch
        {
            // Ignore write failures in protected storage
        }

        return Task.CompletedTask;
    }

    public Task ClearTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        catch
        {
            // Ignore deletion errors
        }

        return Task.CompletedTask;
    }
}
