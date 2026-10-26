using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Persistence contract for user gamification and progression state.
/// </summary>
public interface IGamificationRepository
{
    Task<UserProgressionSnapshot> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(UserProgressionSnapshot snapshot, CancellationToken cancellationToken = default);
}
