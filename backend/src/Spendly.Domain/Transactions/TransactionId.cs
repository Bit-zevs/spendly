using Spendly.Domain.Common;

namespace Spendly.Domain.Transactions;

public readonly record struct TransactionId : IStronglyTypedId<Guid>
{
    public TransactionId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Transaction id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static TransactionId New()
    {
        return new TransactionId(Guid.CreateVersion7());
    }

    public static TransactionId From(Guid value)
    {
        return new TransactionId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
