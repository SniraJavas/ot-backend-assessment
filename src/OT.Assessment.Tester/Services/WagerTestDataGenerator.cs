using Bogus;
using OT.Assessment.Contracts.Message;

namespace OT.Assessment.Tester.Services;

public sealed class WagerTestDataGenerator
{
    private readonly Faker _faker = new();

    private readonly (Guid AccountId, string Username)[] _players;

    public WagerTestDataGenerator()
    {
        _players = Enumerable.Range(1, 5)
            .Select(_ => (
                Guid.NewGuid(),
                _faker.Internet.UserName()))
            .ToArray();
    }

    public List<CasinoWagerMessage> Generate(int count)
    {
        return Enumerable.Range(1, count)
            .Select(_ =>
            {
                var player = _faker.PickRandom(_players);

                return new CasinoWagerMessage
                {
                    WagerId = Guid.NewGuid(),
                    Theme = _faker.PickRandom(
                        "Adventure", "Fantasy", "Classic"),

                    Provider = _faker.PickRandom(
                        "Pragmatic Play", "Evolution", "NetEnt"),

                    GameName = _faker.PickRandom(
                        "Gates of Olympus",
                        "Starburst",
                        "Book of Dead"),

                    TransactionId = Guid.NewGuid(),
                    BrandId = Guid.NewGuid(),

                    AccountId = player.AccountId,
                    Username = player.Username,

                    ExternalReferenceId = Guid.NewGuid(),
                    TransactionTypeId = Guid.NewGuid(),

                    Amount = _faker.Random.Decimal(10m, 1000m),
                    CreatedDateTime = _faker.Date.RecentOffset(7),

                    NumberOfBets = _faker.Random.Int(1, 5),
                    CountryCode = "ZA",
                    Duration = _faker.Random.Long(1, 300)
                };
            })
            .ToList();
    }
}