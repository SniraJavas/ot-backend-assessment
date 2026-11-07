using System;
using System.Collections.Generic;
using System.Text;

namespace OT.Assessment.Contracts.Message
{
    public sealed record CasinoWagerMessage
    {
        public Guid WagerId { get; init; } = Guid.Empty;
        public string Theme { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public string GameName { get; init; } = string.Empty;

        public Guid TransactionId { get; init; } = Guid.Empty;
        public Guid BrandId { get; init; } = Guid.Empty;
        public Guid AccountId { get; init; } = Guid.Empty;
        public string Username { get; init; } = string.Empty;

        public Guid ExternalReferenceId { get; init; } = Guid.Empty;
        public Guid TransactionTypeId { get; init; } = Guid.Empty;

        public decimal Amount { get; init; }
        public DateTimeOffset CreatedDateTime { get; init; }

        public int NumberOfBets { get; init; }
        public string CountryCode { get; init; } = string.Empty;
        public long Duration { get; init; }
    }

}
