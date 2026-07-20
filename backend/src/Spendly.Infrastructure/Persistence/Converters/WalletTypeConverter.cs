using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Spendly.Domain.Wallets;

namespace Spendly.Infrastructure.Persistence.Converters;

internal sealed class WalletTypeConverter
    : ValueConverter<WalletType, short>
{
    public WalletTypeConverter()
        : base(
            walletType => (short)walletType,
            value => (WalletType)value)
    {
    }
}
