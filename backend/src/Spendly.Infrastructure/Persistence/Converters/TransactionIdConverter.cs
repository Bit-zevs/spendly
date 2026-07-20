using Spendly.Domain.Transactions;

namespace Spendly.Infrastructure.Persistence.Converters;

internal sealed class TransactionIdConverter
    : StronglyTypedIdConverter<TransactionId>
{
    public TransactionIdConverter()
        : base(value => TransactionId.From(value))
    {
    }
}
