using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class TestSpotifyTokenStore : ISpotifyTokenStore
{
    public SpotifyToken? StoredToken { get; set; }
    public bool GetTokenCalled { get; private set; }
    public bool SaveTokenCalled { get; private set; }
    public bool ClearTokenCalled { get; private set; }

    public Task<SpotifyToken?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        GetTokenCalled = true;
        return Task.FromResult(StoredToken);
    }

    public Task SaveTokenAsync(SpotifyToken token, CancellationToken cancellationToken = default)
    {
        SaveTokenCalled = true;
        StoredToken = token;
        return Task.CompletedTask;
    }

    public Task ClearTokenAsync(CancellationToken cancellationToken = default)
    {
        ClearTokenCalled = true;
        StoredToken = null;
        return Task.CompletedTask;
    }
}

public class SpotifyServiceTests
{
    [Fact]
    public void Pkce_GenerateCodeVerifier_GeneratesValidLengthAndCharset()
    {
        string verifier = SpotifyPkceAuthHelper.GenerateCodeVerifier();

        Assert.NotNull(verifier);
        Assert.True(verifier.Length >= 43 && verifier.Length <= 128);
        Assert.Matches("^[a-zA-Z0-9-._~]+$", verifier);
    }

    [Fact]
    public void Pkce_GenerateCodeChallenge_ProducesDeterministicBase64Url()
    {
        string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        string challenge1 = SpotifyPkceAuthHelper.GenerateCodeChallenge(verifier);
        string challenge2 = SpotifyPkceAuthHelper.GenerateCodeChallenge(verifier);

        Assert.Equal(challenge1, challenge2);
        Assert.DoesNotContain("+", challenge1);
        Assert.DoesNotContain("/", challenge1);
        Assert.DoesNotContain("=", challenge1);
        Assert.True(challenge1.Length > 0);
    }

    [Fact]
    public void Pkce_BuildAuthorizeUrl_ContainsRequiredOAuthParameters()
    {
        string clientId = "client_abc_123";
        string redirectUri = "http://127.0.0.1:8888/callback/";
        string challenge = "test_challenge";
        string state = "test_state";
        string[] scopes = { "user-read-playback-state", "user-modify-playback-state" };

        string url = SpotifyPkceAuthHelper.BuildAuthorizeUrl(clientId, redirectUri, challenge, state, scopes);

        Assert.StartsWith("https://accounts.spotify.com/authorize?", url);
        Assert.Contains("client_id=client_abc_123", url);
        Assert.Contains("response_type=code", url);
        Assert.Contains("code_challenge_method=S256", url);
        Assert.Contains("code_challenge=test_challenge", url);
        Assert.Contains("state=test_state", url);
        Assert.Contains("user-read-playback-state", url);
    }

    [Fact]
    public void SpotifyToken_IsExpired_IdentifiesExpiredAndFreshTokens()
    {
        var expiredToken = new SpotifyToken
        {
            AccessToken = "expired_token",
            TokenType = "Bearer",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        var freshToken = new SpotifyToken
        {
            AccessToken = "fresh_token",
            TokenType = "Bearer",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(50)
        };

        Assert.True(expiredToken.IsExpired);
        Assert.False(freshToken.IsExpired);
    }

    [Fact]
    public async Task InitializeAsync_WhenStoreHasValidToken_LoadsTokenAndUpdatesAuthState()
    {
        var store = new TestSpotifyTokenStore
        {
            StoredToken = new SpotifyToken
            {
                AccessToken = "stored_access",
                RefreshToken = "stored_refresh",
                TokenType = "Bearer",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            }
        };

        var service = new SpotifyService(store);
        await service.InitializeAsync();

        Assert.True(store.GetTokenCalled);
        Assert.Equal(SpotifyAuthState.Authenticated, service.AuthState);
    }

    [Fact]
    public async Task InitializeAsync_WhenStoreHasTokenWithClientId_RestoresStoredClientId()
    {
        var store = new TestSpotifyTokenStore
        {
            StoredToken = new SpotifyToken
            {
                AccessToken = "stored_access",
                RefreshToken = "stored_refresh",
                TokenType = "Bearer",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                ClientId = "my_persisted_client_id"
            }
        };

        var service = new SpotifyService(store);
        await service.InitializeAsync();

        Assert.Equal("my_persisted_client_id", service.StoredClientId);
    }

    [Fact]
    public async Task InitializeAsync_WhenStoreIsEmpty_RemainsDisconnected()
    {
        var store = new TestSpotifyTokenStore { StoredToken = null };
        var service = new SpotifyService(store);

        await service.InitializeAsync();

        Assert.True(store.GetTokenCalled);
        Assert.Equal(SpotifyAuthState.Disconnected, service.AuthState);
        Assert.Null(service.CurrentUser);
    }

    [Fact]
    public async Task DisconnectAsync_ClearsStoreAndResetsState()
    {
        var store = new TestSpotifyTokenStore
        {
            StoredToken = new SpotifyToken
            {
                AccessToken = "stored_access",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            }
        };

        var service = new SpotifyService(store);
        await service.InitializeAsync();
        Assert.Equal(SpotifyAuthState.Authenticated, service.AuthState);

        await service.DisconnectAsync();

        Assert.True(store.ClearTokenCalled);
        Assert.Equal(SpotifyAuthState.Disconnected, service.AuthState);
        Assert.Null(service.CurrentUser);
        Assert.False(service.CurrentPlayback.IsActive);
    }

    [Fact]
    public async Task GetDevicesAsync_WhenDisconnected_ReturnsEmptyList()
    {
        var store = new TestSpotifyTokenStore { StoredToken = null };
        var service = new SpotifyService(store);

        var devices = await service.GetDevicesAsync();

        Assert.NotNull(devices);
        Assert.Empty(devices);
    }

    [Fact]
    public void SpotifyPlaybackSnapshot_IncludesAvailableDevices()
    {
        var snapshot = new SpotifyPlaybackSnapshot
        {
            IsActive = true,
            AvailableDevices = new[]
            {
                new SpotifyDevice { Id = "dev-1", Name = "Desktop", Type = "Computer", IsActive = true, VolumePercent = 80 },
                new SpotifyDevice { Id = "dev-2", Name = "Phone", Type = "Smartphone", IsActive = false, VolumePercent = 60 }
            }
        };

        Assert.Equal(2, snapshot.AvailableDevices.Count);
        Assert.Equal("Desktop", snapshot.AvailableDevices[0].Name);
        Assert.True(snapshot.AvailableDevices[0].IsActive);
    }
}
