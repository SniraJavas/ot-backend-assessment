using OT.Assessment.Contracts.Message;

namespace OT.Assessment.Consumer.Repositories;

public interface IWagerRepository
{
    Task<bool> InsertAsync(
        CasinoWagerMessage message,
        CancellationToken cancellationToken);
}