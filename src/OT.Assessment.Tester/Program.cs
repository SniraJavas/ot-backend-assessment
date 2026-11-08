using Microsoft.Extensions.Configuration;
using NBomber.CSharp;
using OT.Assessment.Tester.Services;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var endpoint = configuration["Api:EndpointUri"]
    ?? throw new InvalidOperationException("API endpoint is missing.");

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(endpoint)
};

var apiClient = new WagerApiClient(httpClient);
var generator = new WagerTestDataGenerator();

var scenario = Scenario.Create("submit_casino_wagers", async context =>
{
    var wager = generator.Generate(1).Single();

    try
    {
        using var response = await apiClient.SubmitAsync(
            wager,
            CancellationToken.None);

        if (response.IsSuccessStatusCode)
            return Response.Ok();

        return Response.Fail(
            statusCode: ((int)response.StatusCode).ToString(),
            message: $"API returned HTTP {(int)response.StatusCode}");
    }
    catch (Exception exception)
    {
        return Response.Fail(
            statusCode: "exception",
            message: exception.Message);
    }
})
.WithLoadSimulations(
    Simulation.KeepConstant(
        copies: 1,
        during: TimeSpan.FromSeconds(10)));

NBomberRunner
    .RegisterScenarios(scenario)
    .Run();