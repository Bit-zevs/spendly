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

        public static readonly DomainError CurrencyMismatch = new(
            "Money.Currency.Mismatch",
            "Money operations require the same currency.");
    }

    public static class Wallet
    {
        public static readonly DomainError NameIsEmpty = new(
            "Wallet.Name.Empty",
            "Wallet name cannot be empty.");

        public static readonly DomainError TypeIsInvalid = new(
            "Wallet.Type.Invalid",
            "Wallet type must be one of the supported values.");

        public static readonly DomainError CurrencyIsRequired = new(
            "Wallet.Currency.Required",
            "Wallet currency is required.");

        public static readonly DomainError CreatedAtIsInvalid = new(
            "Wallet.CreatedAt.Invalid",
            "Wallet creation time must not be the default value.");
    }

    public static class Category
    {
        public static readonly DomainError NameIsEmpty = new(
            "Category.Name.Empty",
            "Category name cannot be empty.");

        public static readonly DomainError TypeIsInvalid = new(
            "Category.Type.Invalid",
            "Category type must be one of the supported values.");

        public static readonly DomainError CreatedAtIsInvalid = new(
            "Category.CreatedAt.Invalid",
            "Category creation time must not be the default value.");
    }

    public static class Transaction
    {
        public static readonly DomainError TypeIsInvalid = new(
            "Transaction.Type.Invalid",
            "Transaction type must be one of the defined values.");

        public static readonly DomainError TransferIsNotSupported = new(
            "Transaction.Transfer.NotSupported",
            "Transfer transactions are not supported by the current transaction model.");

        public static readonly DomainError AmountIsRequired = new(
            "Transaction.Amount.Required",
            "Transaction amount is required.");

        public static readonly DomainError AmountMustBePositive = new(
            "Transaction.Amount.NotPositive",
            "Transaction amount must be greater than zero.");

        public static readonly DomainError WalletIsRequired = new(
            "Transaction.Wallet.Required",
            "Transaction wallet is required.");

        public static readonly DomainError CategoryIsRequired = new(
            "Transaction.Category.Required",
            "Income and expense transactions require a category.");

        public static readonly DomainError CategoryTypeMismatch = new(
            "Transaction.Category.TypeMismatch",
            "Transaction category type must match the transaction type.");

        public static readonly DomainError OccurredAtIsInvalid = new(
            "Transaction.OccurredAt.Invalid",
            "Transaction occurrence time must not be the default value.");

        public static readonly DomainError CreatedAtIsInvalid = new(
            "Transaction.CreatedAt.Invalid",
            "Transaction creation time must not be the default value.");
    }
}
