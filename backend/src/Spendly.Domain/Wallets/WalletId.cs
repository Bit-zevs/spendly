using Spendly.Domain.Common;

namespace Spendly.Domain.Wallets;

public readonly record struct WalletId : IStronglyTypedId<Guid>
{
    public WalletId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Wallet id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static WalletId New()
    {
        return new WalletId(Guid.CreateVersion7());
    }

    public static WalletId From(Guid value)
    {
        return new WalletId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
