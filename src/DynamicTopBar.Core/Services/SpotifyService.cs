using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Direct Spotify Web API client implementing Authorization Code with PKCE and playback controls.
/// </summary>
public sealed class SpotifyService : ISpotifyService
{
    private const string AccountsTokenEndpoint = "https://accounts.spotify.com/api/token";
    private const string ApiBaseUrl = "https://api.spotify.com/v1";
    private const string DefaultScopes = "user-read-playback-state user-modify-playback-state user-read-currently-playing user-read-private user-read-email";

    private readonly HttpClient _httpClient;
    private readonly ISpotifyTokenStore _tokenStore;
    private readonly IProcessLauncher? _processLauncher;
    private readonly object _lock = new();

    private SpotifyAuthState _authState = SpotifyAuthState.Disconnected;
    private SpotifyUserProfile? _currentUser;
    private SpotifyPlaybackSnapshot _currentPlayback = SpotifyPlaybackSnapshot.Empty();
    private SpotifyToken? _currentToken;
    private string? _storedClientId;
    private bool _isDisposed;

    public SpotifyAuthState AuthState
    {
        get { lock (_lock) return _authState; }
        private set
        {
            lock (_lock) _authState = value;
            AuthStateChanged?.Invoke(this, value);
        }
    }

    public SpotifyUserProfile? CurrentUser
    {
        get { lock (_lock) return _currentUser; }
        private set { lock (_lock) _currentUser = value; }
    }

    public SpotifyPlaybackSnapshot CurrentPlayback
    {
        get { lock (_lock) return _currentPlayback; }
        private set
        {
            lock (_lock) _currentPlayback = value;
            PlaybackChanged?.Invoke(this, value);
        }
    }

    public string? StoredClientId => _storedClientId;

    public event EventHandler<SpotifyAuthState>? AuthStateChanged;
    public event EventHandler<SpotifyPlaybackSnapshot>? PlaybackChanged;

    public SpotifyService(ISpotifyTokenStore tokenStore, HttpClient? httpClient = null, IProcessLauncher? processLauncher = null)
    {
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _httpClient = httpClient ?? new HttpClient();
        _processLauncher = processLauncher;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var storedToken = await _tokenStore.GetTokenAsync(cancellationToken);
            if (storedToken != null)
            {
                _currentToken = storedToken;
                if (!string.IsNullOrWhiteSpace(_currentToken.ClientId))
                {
                    _storedClientId = _currentToken.ClientId;
                }

                if (_currentToken.IsExpired)
                {
                    if (!string.IsNullOrWhiteSpace(_currentToken.RefreshToken) && !string.IsNullOrWhiteSpace(_storedClientId))
                    {
                        bool refreshed = await RefreshAccessTokenAsync(_storedClientId, cancellationToken);
                        if (!refreshed)
                        {
                            AuthState = SpotifyAuthState.TokenExpired;
                            return;
                        }
                    }
                    else
                    {
                        AuthState = SpotifyAuthState.TokenExpired;
                        return;
                    }
                }

                await FetchUserProfileAsync(cancellationToken);
                await RefreshPlaybackAsync(cancellationToken);
                AuthState = SpotifyAuthState.Authenticated;
            }
            else
            {
                AuthState = SpotifyAuthState.Disconnected;
            }
        }
        catch (Exception ex)
        {
            AuthState = SpotifyAuthState.Error;
            CurrentPlayback = SpotifyPlaybackSnapshot.Empty($"Init error: {ex.Message}");
        }
    }

    public async Task<bool> StartPkceAuthAsync(string clientId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        _storedClientId = clientId.Trim();

        AuthState = SpotifyAuthState.Connecting;

        string codeVerifier = SpotifyPkceAuthHelper.GenerateCodeVerifier();
        string codeChallenge = SpotifyPkceAuthHelper.GenerateCodeChallenge(codeVerifier);
        string state = Guid.NewGuid().ToString("N");

        const int port = 8888;
        string redirectUri = $"http://127.0.0.1:{port}/callback/";

        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);

        try
        {
            listener.Start();
        }
        catch (Exception ex)
        {
            AuthState = SpotifyAuthState.Error;
            CurrentPlayback = SpotifyPlaybackSnapshot.Empty($"Could not start OAuth loopback listener: {ex.Message}");
            return false;
        }

        string authUrl = SpotifyPkceAuthHelper.BuildAuthorizeUrl(_storedClientId, redirectUri, codeChallenge, DefaultScopes, state);

        if (_processLauncher != null)
        {
            _processLauncher.Launch(authUrl);
        }
        else
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });
        }

        HttpListenerContext context;
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(2));

            var contextTask = listener.GetContextAsync();
            var completedTask = await Task.WhenAny(contextTask, Task.Delay(-1, timeoutCts.Token));

            if (completedTask != contextTask)
            {
                listener.Stop();
                AuthState = SpotifyAuthState.Disconnected;
                CurrentPlayback = SpotifyPlaybackSnapshot.Empty("Authentication timed out or cancelled.");
                return false;
            }

            context = await contextTask;
        }
        catch (OperationCanceledException)
        {
            listener.Stop();
            AuthState = SpotifyAuthState.Disconnected;
            CurrentPlayback = SpotifyPlaybackSnapshot.Empty("Authentication cancelled.");
            return false;
        }
        catch (Exception ex)
        {
            listener.Stop();
            AuthState = SpotifyAuthState.Error;
            CurrentPlayback = SpotifyPlaybackSnapshot.Empty($"OAuth listener error: {ex.Message}");
            return false;
        }

        var req = context.Request;
        var query = req.QueryString;
        string? returnedCode = query["code"];
        string? returnedState = query["state"];
        string? error = query["error"];

        // Respond to user browser cleanly
        var res = context.Response;
        string responseHtml = "<html><body style='font-family:sans-serif;background:#090909;color:#F5F5F5;padding:40px;text-align:center;'><h2>DynamicTopBar</h2><p>Spotify authorization complete. You can close this window.</p></body></html>";
        byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
        res.ContentLength64 = buffer.Length;
        res.ContentType = "text/html";
        using (var output = res.OutputStream)
        {
            await output.WriteAsync(buffer, cancellationToken);
        }
        listener.Stop();

        if (!string.IsNullOrEmpty(error) || returnedCode == null || returnedState != state)
        {
            AuthState = SpotifyAuthState.Error;
            CurrentPlayback = SpotifyPlaybackSnapshot.Empty($"Spotify authorization rejected: {error ?? "Invalid state"}");
            return false;
        }

        bool tokenExchanged = await ExchangeCodeForTokenAsync(returnedCode, _storedClientId, redirectUri, codeVerifier, cancellationToken);
        if (!tokenExchanged)
        {
            AuthState = SpotifyAuthState.Error;
            return false;
        }

        await FetchUserProfileAsync(cancellationToken);
        await RefreshPlaybackAsync(cancellationToken);
        AuthState = SpotifyAuthState.Authenticated;
        return true;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _currentToken = null;
        CurrentUser = null;
        CurrentPlayback = SpotifyPlaybackSnapshot.Empty("Disconnected from Spotify");
        await _tokenStore.ClearTokenAsync(cancellationToken);
        AuthState = SpotifyAuthState.Disconnected;
    }

    public async Task<SpotifyPlaybackSnapshot> RefreshPlaybackAsync(CancellationToken cancellationToken = default)
    {
        if (_currentToken == null)
        {
            return SpotifyPlaybackSnapshot.Empty("Not authenticated with Spotify");
        }

        if (_currentToken.IsExpired && !string.IsNullOrWhiteSpace(_storedClientId) && !string.IsNullOrWhiteSpace(_currentToken.RefreshToken))
        {
            bool refreshed = await RefreshAccessTokenAsync(_storedClientId, cancellationToken);
            if (!refreshed)
            {
                AuthState = SpotifyAuthState.TokenExpired;
                return SpotifyPlaybackSnapshot.Empty("Spotify token expired. Re-authentication required.");
            }
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/me/player");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentToken.AccessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                var empty = SpotifyPlaybackSnapshot.Empty("No active playback on Spotify devices");
                CurrentPlayback = empty;
                return empty;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                AuthState = SpotifyAuthState.TokenExpired;
                var expired = SpotifyPlaybackSnapshot.Empty("Authentication expired. Please reconnect.");
                CurrentPlayback = expired;
                return expired;
            }

            if (response.StatusCode == (HttpStatusCode)429) // Rate limit
            {
                var snapshot = CurrentPlayback with { StatusMessage = "Spotify rate limit reached. Waiting..." };
                CurrentPlayback = snapshot;
                return snapshot;
            }

            if (!response.IsSuccessStatusCode)
            {
                var err = SpotifyPlaybackSnapshot.Empty($"Spotify API error: {(int)response.StatusCode}");
                CurrentPlayback = err;
                return err;
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            bool isPlaying = root.TryGetProperty("is_playing", out var isPlayingProp) && isPlayingProp.GetBoolean();
            string? deviceName = null;
            if (root.TryGetProperty("device", out var deviceProp) && deviceProp.TryGetProperty("name", out var dNameProp))
            {
                deviceName = dNameProp.GetString();
            }

            SpotifyTrack? track = null;
            if (root.TryGetProperty("item", out var itemProp) && itemProp.ValueKind == JsonValueKind.Object)
            {
                string trackId = itemProp.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                string trackName = itemProp.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Unknown Title" : "Unknown Title";

                string artistName = "Unknown Artist";
                if (itemProp.TryGetProperty("artists", out var artistsProp) && artistsProp.GetArrayLength() > 0)
                {
                    artistName = artistsProp[0].TryGetProperty("name", out var aNameProp) ? aNameProp.GetString() ?? artistName : artistName;
                }

                string albumName = "";
                string? artworkUrl = null;
                if (itemProp.TryGetProperty("album", out var albumProp))
                {
                    albumName = albumProp.TryGetProperty("name", out var albNameProp) ? albNameProp.GetString() ?? "" : "";
                    if (albumProp.TryGetProperty("images", out var imagesProp) && imagesProp.GetArrayLength() > 0)
                    {
                        artworkUrl = imagesProp[0].TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                    }
                }

                long durationMs = itemProp.TryGetProperty("duration_ms", out var durProp) ? durProp.GetInt64() : 0;

                track = new SpotifyTrack
                {
                    Id = trackId,
                    Name = trackName,
                    Artist = artistName,
                    Album = albumName,
                    ArtworkUrl = artworkUrl,
                    Duration = TimeSpan.FromMilliseconds(durationMs),
                    IsPlaying = isPlaying
                };
            }

            long progressMs = root.TryGetProperty("progress_ms", out var progProp) ? progProp.GetInt64() : 0;

            bool isUserPremium = CurrentUser?.IsPremium ?? true; // If unknown assume allowed or checked on API response

            var devices = await GetDevicesAsync(cancellationToken);

            var updatedSnapshot = new SpotifyPlaybackSnapshot
            {
                IsActive = true,
                IsPlaying = isPlaying,
                DeviceName = deviceName,
                Track = track,
                Position = TimeSpan.FromMilliseconds(progressMs),
                CanPlayPause = true,
                CanSkipNext = true,
                CanSkipPrevious = true,
                RequiresPremiumNotice = !isUserPremium,
                StatusMessage = !isUserPremium 
                    ? "Spotify Web API requires Premium. Media Player (GSMTC) supports free desktop app." 
                    : null,
                AvailableDevices = devices
            };

            CurrentPlayback = updatedSnapshot;
            return updatedSnapshot;
        }
        catch (Exception ex)
        {
            var err = SpotifyPlaybackSnapshot.Empty($"Playback fetch error: {ex.Message}");
            CurrentPlayback = err;
            return err;
        }
    }

    public async Task<IReadOnlyList<SpotifyDevice>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        if (_currentToken == null) return Array.Empty<SpotifyDevice>();

        if (_currentToken.IsExpired && !string.IsNullOrWhiteSpace(_storedClientId))
        {
            await RefreshAccessTokenAsync(_storedClientId, cancellationToken);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/me/player/devices");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentToken.AccessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<SpotifyDevice>();
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("devices", out var devicesProp) || devicesProp.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<SpotifyDevice>();
            }

            var devices = new List<SpotifyDevice>();
            foreach (var item in devicesProp.EnumerateArray())
            {
                string id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                string name = item.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? "Unknown" : "Unknown";
                string type = item.TryGetProperty("type", out var tProp) ? tProp.GetString() ?? "Device" : "Device";
                bool isActive = item.TryGetProperty("is_active", out var aProp) && aProp.GetBoolean();
                bool isRestricted = item.TryGetProperty("is_restricted", out var rProp) && rProp.GetBoolean();
                int? volume = item.TryGetProperty("volume_percent", out var vProp) && vProp.ValueKind == JsonValueKind.Number ? vProp.GetInt32() : null;

                if (!string.IsNullOrEmpty(id))
                {
                    devices.Add(new SpotifyDevice
                    {
                        Id = id,
                        Name = name,
                        Type = type,
                        IsActive = isActive,
                        IsRestricted = isRestricted,
                        VolumePercent = volume
                    });
                }
            }

            return devices;
        }
        catch
        {
            return Array.Empty<SpotifyDevice>();
        }
    }

    public async Task<bool> TransferPlaybackAsync(string deviceId, bool play = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        if (_currentToken == null) return false;

        if (_currentToken.IsExpired && !string.IsNullOrWhiteSpace(_storedClientId))
        {
            await RefreshAccessTokenAsync(_storedClientId, cancellationToken);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"{ApiBaseUrl}/me/player");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentToken.AccessToken);

            var payload = new
            {
                device_ids = new[] { deviceId },
                play = play
            };
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(400, cancellationToken);
                    await RefreshPlaybackAsync(cancellationToken);
                }, cancellationToken);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> TogglePlayPauseAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentPlayback.IsPlaying)
        {
            return await SendPlayerCommandAsync(HttpMethod.Put, "pause", cancellationToken);
        }
        else
        {
            return await SendPlayerCommandAsync(HttpMethod.Put, "play", cancellationToken);
        }
    }

    public async Task<bool> SkipNextAsync(CancellationToken cancellationToken = default)
    {
        return await SendPlayerCommandAsync(HttpMethod.Post, "next", cancellationToken);
    }

    public async Task<bool> SkipPreviousAsync(CancellationToken cancellationToken = default)
    {
        return await SendPlayerCommandAsync(HttpMethod.Post, "previous", cancellationToken);
    }

    private async Task<bool> SendPlayerCommandAsync(HttpMethod method, string endpoint, CancellationToken cancellationToken)
    {
        if (_currentToken == null) return false;

        try
        {
            using var request = new HttpRequestMessage(method, $"{ApiBaseUrl}/me/player/{endpoint}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentToken.AccessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                CurrentPlayback = CurrentPlayback with
                {
                    RequiresPremiumNotice = true,
                    StatusMessage = "Spotify Web API playback control requires Spotify Premium."
                };
                return false;
            }

            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent)
            {
                // Refresh state after slight delay for Spotify backend synchronization
                _ = Task.Run(async () =>
                {
                    await Task.Delay(300, cancellationToken);
                    await RefreshPlaybackAsync(cancellationToken);
                }, cancellationToken);
                return true;
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> ExchangeCodeForTokenAsync(string code, string clientId, string redirectUri, string codeVerifier, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = clientId,
                ["code_verifier"] = codeVerifier
            };

            using var content = new FormUrlEncodedContent(parameters);
            using var response = await _httpClient.PostAsync(AccountsTokenEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errText = await response.Content.ReadAsStringAsync(cancellationToken);
                CurrentPlayback = SpotifyPlaybackSnapshot.Empty($"Token exchange failed: {response.StatusCode} {errText}");
                return false;
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string accessToken = root.GetProperty("access_token").GetString()!;
            string? refreshToken = root.TryGetProperty("refresh_token", out var rProp) ? rProp.GetString() : null;
            int expiresIn = root.GetProperty("expires_in").GetInt32();
            string? scope = root.TryGetProperty("scope", out var sProp) ? sProp.GetString() : null;

            _currentToken = new SpotifyToken
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn),
                Scope = scope,
                ClientId = clientId
            };

            await _tokenStore.SaveTokenAsync(_currentToken, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            CurrentPlayback = SpotifyPlaybackSnapshot.Empty($"Token exchange exception: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RefreshAccessTokenAsync(string clientId, CancellationToken cancellationToken)
    {
        if (_currentToken?.RefreshToken == null) return false;

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = _currentToken.RefreshToken,
                ["client_id"] = clientId
            };

            using var content = new FormUrlEncodedContent(parameters);
            using var response = await _httpClient.PostAsync(AccountsTokenEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string accessToken = root.GetProperty("access_token").GetString()!;
            string? newRefreshToken = root.TryGetProperty("refresh_token", out var rProp) ? rProp.GetString() : _currentToken.RefreshToken;
            int expiresIn = root.GetProperty("expires_in").GetInt32();

            _currentToken = new SpotifyToken
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn),
                Scope = _currentToken.Scope,
                ClientId = clientId
            };

            await _tokenStore.SaveTokenAsync(_currentToken, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task FetchUserProfileAsync(CancellationToken cancellationToken)
    {
        if (_currentToken == null) return;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentToken.AccessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string id = root.GetProperty("id").GetString()!;
                string displayName = root.TryGetProperty("display_name", out var dnProp) ? dnProp.GetString() ?? id : id;
                string? email = root.TryGetProperty("email", out var eProp) ? eProp.GetString() : null;
                string? product = root.TryGetProperty("product", out var pProp) ? pProp.GetString() : "unknown";

                CurrentUser = new SpotifyUserProfile
                {
                    Id = id,
                    DisplayName = displayName,
                    Email = email,
                    Product = product
                };
            }
        }
        catch
        {
            // Ignore failure to fetch user profile, playback is primary
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
