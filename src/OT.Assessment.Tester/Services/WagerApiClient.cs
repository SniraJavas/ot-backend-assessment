using System.Net.Http.Json;
using OT.Assessment.Contracts.Message;

namespace OT.Assessment.Tester.Services;

public sealed class WagerApiClient(HttpClient httpClient)
{
    public async Task<HttpResponseMessage> SubmitAsync(
        CasinoWagerMessage wager,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsJsonAsync(
            string.Empty,
            wager,
            cancellationToken);
    }
}