
namespace OT.Assessment.App.Models;

public sealed class TopSpenderDto
{
    public Guid AccountId { get; init; }
    public string Username { get; init; } = string.Empty;
    public decimal TotalAmountSpend { get; init; }
}
