using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.Wallets;

namespace Spendly.IntegrationTests.Persistence.Compatibility;

internal static class CompatibilityValueConverters
{
    public static ValueConverter<WalletType, short> WalletTypeToInt16 { get; } =
        new(
            type => (short)type,
            value => (WalletType)value);

    public static ValueConverter<CategoryType, short> CategoryTypeToInt16 { get; } =
        new(
            type => (short)type,
            value => (CategoryType)value);

    public static ValueConverter<TransactionType, short> TransactionTypeToInt16 { get; } =
        new(
            type => (short)type,
            value => (TransactionType)value);
}
