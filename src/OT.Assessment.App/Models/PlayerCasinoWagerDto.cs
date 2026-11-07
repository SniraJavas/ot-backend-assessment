
namespace OT.Assessment.App.Models;

public sealed class PlayerCasinoWagerDto
{
    public Guid WagerId { get; init; }
    public string Game { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
}
