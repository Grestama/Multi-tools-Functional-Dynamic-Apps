using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Secure storage contract for Spotify OAuth tokens.
/// </summary>
public interface ISpotifyTokenStore
{
    Task<SpotifyToken?> GetTokenAsync(CancellationToken cancellationToken = default);
    Task SaveTokenAsync(SpotifyToken token, CancellationToken cancellationToken = default);
    Task ClearTokenAsync(CancellationToken cancellationToken = default);
}
