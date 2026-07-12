using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;

namespace Spendly.IntegrationTests.Persistence.Compatibility;

internal static class CompatibilityValueConverters
{
    public static ValueConverter<WalletId, Guid> WalletIdToGuid { get; } =
        new(
            id => id.Value,
            value => WalletId.From(value));

    public static ValueConverter<CategoryId, Guid> CategoryIdToGuid { get; } =
        new(
            id => id.Value,
            value => CategoryId.From(value));

    public static ValueConverter<TransactionId, Guid> TransactionIdToGuid { get; } =
        new(
            id => id.Value,
            value => TransactionId.From(value));

    public static ValueConverter<Currency, string> CurrencyToCode { get; } =
        new(
            currency => currency.Code,
            code => Currency.From(code));
}
