
using System.Data;
using Dapper;
using OT.Assessment.App.Data;
using OT.Assessment.App.Models;

namespace OT.Assessment.App.Repositories;

public sealed class CasinoWagerRepository(
    IDbConnectionFactory connectionFactory) : ICasinoWagerRepository
{
    public async Task<PagedResponse<PlayerCasinoWagerDto>>
        GetPlayerCasinoWagersAsync(
            Guid playerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var parameters = new
        {
            AccountId = playerId,
            Page = page,
            PageSize = pageSize
        };

        var command = new CommandDefinition(
            commandText: "dbo.usp_PlayerCasinoWagers_GetPaged",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        using var results = await connection.QueryMultipleAsync(command);

        var wagers = (await results.ReadAsync<PlayerCasinoWagerDto>())
            .AsList();

        var total = await results.ReadSingleAsync<int>();

        return new PagedResponse<PlayerCasinoWagerDto>
        {
            Data = wagers,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = (int)Math.Ceiling(
                total / (double)pageSize)
        };
    }

    public async Task<IReadOnlyList<TopSpenderDto>> GetTopSpendersAsync(
        int count,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            commandText: "dbo.usp_PlayerTopSpenders_Get",
            parameters: new { Count = count },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var results = await connection.QueryAsync<TopSpenderDto>(command);

        return results.AsList();
    }
}
