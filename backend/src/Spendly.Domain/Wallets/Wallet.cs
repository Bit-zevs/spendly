using Spendly.Domain.Common;
using Spendly.Domain.Errors;
using Spendly.Domain.ValueObjects;

namespace Spendly.Domain.Wallets;

public sealed class Wallet : Entity<WalletId>
{
    private Wallet(
        WalletId id,
        string name,
        WalletType type,
        Currency currency,
        DateTimeOffset createdAt)
        : base(id)
    {
        Name = name;
        Type = type;
        Currency = currency;
        CreatedAt = createdAt;
    }

    public string Name { get; }

    public WalletType Type { get; }

    public Currency Currency { get; }

    public DateTimeOffset CreatedAt { get; }

    public static Wallet Create(
        string? name,
        WalletType type,
        Currency? currency,
        DateTimeOffset createdAt)
    {
        var normalizedName = NormalizeName(name);

        EnsureTypeIsValid(type);

        var requiredCurrency = EnsureCurrencyIsProvided(currency);
        var utcCreatedAt = NormalizeCreatedAt(createdAt);

        return new Wallet(
            WalletId.New(),
            normalizedName,
            type,
            requiredCurrency,
            utcCreatedAt);
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(DomainErrors.Wallet.NameIsEmpty);
        }

        return name.Trim();
    }

    private static void EnsureTypeIsValid(WalletType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException(DomainErrors.Wallet.TypeIsInvalid);
        }
    }

    private static Currency EnsureCurrencyIsProvided(Currency? currency)
    {
        return currency
               ?? throw new DomainException(DomainErrors.Wallet.CurrencyIsRequired);
    }

    private static DateTimeOffset NormalizeCreatedAt(DateTimeOffset createdAt)
    {
        if (createdAt == default)
        {
            throw new DomainException(DomainErrors.Wallet.CreatedAtIsInvalid);
        }

        return createdAt.ToUniversalTime();
    }
}
