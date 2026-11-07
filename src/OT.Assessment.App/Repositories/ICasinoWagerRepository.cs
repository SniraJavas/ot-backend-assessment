
using OT.Assessment.App.Models;

namespace OT.Assessment.App.Repositories;

public interface ICasinoWagerRepository
{
    Task<PagedResponse<PlayerCasinoWagerDto>> GetPlayerCasinoWagersAsync(
        Guid playerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TopSpenderDto>> GetTopSpendersAsync(
        int count,
        CancellationToken cancellationToken);
}
