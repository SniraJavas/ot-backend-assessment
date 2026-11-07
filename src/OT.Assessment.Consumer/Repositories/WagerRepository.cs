using Dapper;
using OT.Assessment.Consumer.Data;
using OT.Assessment.Contracts.Message;
using System.Data;

namespace OT.Assessment.Consumer.Repositories;

public sealed class WagerRepository(
    IDbConnectionFactory connectionFactory) : IWagerRepository
{
    public async Task<bool> InsertAsync(
        CasinoWagerMessage message,
        CancellationToken cancellationToken)
    {
        await using var connection =
            connectionFactory.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var parameters = new
        {
            message.WagerId,
            message.Theme,
            ProviderName = message.Provider,
            message.GameName,
            message.TransactionId,
            message.BrandId,
            message.AccountId,
            message.Username,
            message.ExternalReferenceId,
            message.TransactionTypeId,
            message.Amount,
            message.CreatedDateTime,
            message.NumberOfBets,
            message.CountryCode,
            message.Duration
        };

        var command = new CommandDefinition(
            commandText: "dbo.usp_CasinoWager_Insert",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var wasInserted = await connection.QuerySingleAsync<int>(command);

        return wasInserted == 1;
    }
}