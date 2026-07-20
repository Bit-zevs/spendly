using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Spendly.Domain.Transactions;

namespace Spendly.Infrastructure.Persistence.Converters;

internal sealed class TransactionTypeConverter
    : ValueConverter<TransactionType, short>
{
    public TransactionTypeConverter()
        : base(
            transactionType => (short)transactionType,
            value => (TransactionType)value)
    {
    }
}
