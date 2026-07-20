using Spendly.Domain.Wallets;

namespace Spendly.Infrastructure.Persistence.Converters;

internal sealed class WalletIdConverter
    : StronglyTypedIdConverter<WalletId>
{
    public WalletIdConverter()
        : base(value => WalletId.From(value))
    {
    }
}
