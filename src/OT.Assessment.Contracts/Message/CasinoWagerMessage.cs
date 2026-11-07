using System;
using System.Collections.Generic;
using System.Text;

namespace OT.Assessment.Contracts.Message
{
    public sealed record CasinoWagerMessage
    {
        public string WagerId { get; init; } = string.Empty;
        public string Theme { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public string GameName { get; init; } = string.Empty;

        public string TransactionId { get; init; } = string.Empty;
        public string BrandId { get; init; } = string.Empty;
        public string AccountId { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;

        public string ExternalReferenceId { get; init; } = string.Empty;
        public string TransactionTypeId { get; init; } = string.Empty;

        public decimal Amount { get; init; }
        public DateTimeOffset CreatedDateTime { get; init; }

        public int NumberOfBets { get; init; }
        public string CountryCode { get; init; } = string.Empty;
        public long Duration { get; init; }
    }

}
