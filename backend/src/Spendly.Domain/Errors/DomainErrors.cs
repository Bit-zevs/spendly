namespace Spendly.Domain.Errors;

/// <summary>
/// Contains reusable domain errors for known business rule violations.
/// </summary>
public static class DomainErrors
{
    public static class Currency
    {
        public static readonly DomainError CodeIsRequired = new(
            "Currency.Code.Required",
            "Currency code is required.");

        public static readonly DomainError CodeHasInvalidFormat = new(
            "Currency.Code.InvalidFormat",
            "Currency code must contain exactly three Latin letters.");
    }

    public static class Money
    {
        public static readonly DomainError AmountIsNegative = new(
            "Money.Amount.Negative",
            "Money amount cannot be negative.");

        public static readonly DomainError AmountMustBePositive = new(
            "Money.Amount.NotPositive",
            "Money amount must be greater than zero.");

        public static readonly DomainError CurrencyIsRequired = new(
            "Money.Currency.Required",
            "Money currency is required.");
    }

    public static class Wallet
    {
        public static readonly DomainError NameIsEmpty = new(
            "Wallet.Name.Empty",
            "Wallet name cannot be empty.");
    }

    public static class Category
    {
        public static readonly DomainError NameIsEmpty = new(
            "Category.Name.Empty",
            "Category name cannot be empty.");
    }

    public static class Transaction
    {
        public static readonly DomainError AmountMustBePositive = new(
            "Transaction.Amount.NotPositive",
            "Transaction amount must be greater than zero.");

        public static readonly DomainError WalletIsRequired = new(
            "Transaction.Wallet.Required",
            "Transaction wallet is required.");
    }
}
