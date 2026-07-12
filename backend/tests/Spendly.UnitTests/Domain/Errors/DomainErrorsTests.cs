using System.Reflection;
using Spendly.Domain.Errors;

namespace Spendly.UnitTests.Domain.Errors;

public sealed class DomainErrorsTests
{
    [Fact]
    public void KnownErrors_ShouldHaveStableCodes()
    {
        Assert.Equal(
            "Currency.Code.Required",
            DomainErrors.Currency.CodeIsRequired.Code);

        Assert.Equal(
            "Currency.Code.InvalidFormat",
            DomainErrors.Currency.CodeHasInvalidFormat.Code);

        Assert.Equal(
            "Money.Amount.Negative",
            DomainErrors.Money.AmountIsNegative.Code);

        Assert.Equal(
            "Money.Amount.NotPositive",
            DomainErrors.Money.AmountMustBePositive.Code);

        Assert.Equal(
            "Money.Amount.ExceedsMaximum",
            DomainErrors.Money.AmountExceedsMaximum.Code);

        Assert.Equal(
            "Money.Amount.TooManyFractionalDigits",
            DomainErrors.Money.AmountHasTooManyFractionalDigits.Code);

        Assert.Equal(
            "Money.Currency.Required",
            DomainErrors.Money.CurrencyIsRequired.Code);

        Assert.Equal(
            "Money.Currency.Mismatch",
            DomainErrors.Money.CurrencyMismatch.Code);

        Assert.Equal(
            "Wallet.Name.Empty",
            DomainErrors.Wallet.NameIsEmpty.Code);

        Assert.Equal(
            "Wallet.Name.TooLong",
            DomainErrors.Wallet.NameIsTooLong.Code);

        Assert.Equal(
            "Wallet.Type.Invalid",
            DomainErrors.Wallet.TypeIsInvalid.Code);

        Assert.Equal(
            "Wallet.Currency.Required",
            DomainErrors.Wallet.CurrencyIsRequired.Code);

        Assert.Equal(
            "Wallet.CreatedAt.Invalid",
            DomainErrors.Wallet.CreatedAtIsInvalid.Code);

        Assert.Equal(
            "Category.Name.Empty",
            DomainErrors.Category.NameIsEmpty.Code);

        Assert.Equal(
            "Category.Name.TooLong",
            DomainErrors.Category.NameIsTooLong.Code);

        Assert.Equal(
            "Category.Type.Invalid",
            DomainErrors.Category.TypeIsInvalid.Code);

        Assert.Equal(
            "Category.CreatedAt.Invalid",
            DomainErrors.Category.CreatedAtIsInvalid.Code);

        Assert.Equal(
            "Transaction.Type.Invalid",
            DomainErrors.Transaction.TypeIsInvalid.Code);

        Assert.Equal(
            "Transaction.Transfer.NotSupported",
            DomainErrors.Transaction.TransferIsNotSupported.Code);

        Assert.Equal(
            "Transaction.Amount.Required",
            DomainErrors.Transaction.AmountIsRequired.Code);

        Assert.Equal(
            "Transaction.Amount.NotPositive",
            DomainErrors.Transaction.AmountMustBePositive.Code);

        Assert.Equal(
            "Transaction.Amount.CurrencyMismatch",
            DomainErrors.Transaction.AmountCurrencyMismatch.Code);

        Assert.Equal(
            "Transaction.Wallet.Required",
            DomainErrors.Transaction.WalletIsRequired.Code);

        Assert.Equal(
            "Transaction.Category.Required",
            DomainErrors.Transaction.CategoryIsRequired.Code);

        Assert.Equal(
            "Transaction.Category.TypeMismatch",
            DomainErrors.Transaction.CategoryTypeMismatch.Code);

        Assert.Equal(
            "Transaction.OccurredAt.Invalid",
            DomainErrors.Transaction.OccurredAtIsInvalid.Code);

        Assert.Equal(
            "Transaction.Description.TooLong",
            DomainErrors.Transaction.DescriptionIsTooLong.Code);

        Assert.Equal(
            "Transaction.CreatedAt.Invalid",
            DomainErrors.Transaction.CreatedAtIsInvalid.Code);
    }

    [Fact]
    public void KnownErrors_ShouldHaveNonEmptyCodesAndMessages()
    {
        var errors = GetKnownErrors();

        Assert.NotEmpty(errors);

        foreach (var error in errors)
        {
            Assert.False(string.IsNullOrWhiteSpace(error.Code));
            Assert.False(string.IsNullOrWhiteSpace(error.Message));
        }
    }

    [Fact]
    public void KnownErrors_ShouldHaveUniqueCodes()
    {
        var errors = GetKnownErrors();

        var duplicateCodes = errors
            .GroupBy(error => error.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.Empty(duplicateCodes);
    }

    [Fact]
    public void KnownErrorCodes_ShouldFollowDomainCodeConvention()
    {
        var errors = GetKnownErrors();

        foreach (var error in errors)
        {
            var codeParts = error.Code.Split('.');

            Assert.True(
                codeParts.Length >= 3,
                $"Domain error code '{error.Code}' must contain at least three dot-separated parts.");

            Assert.All(
                codeParts,
                part => Assert.Matches("^[A-Z][A-Za-z0-9]*$", part));
        }
    }

    private static IReadOnlyList<DomainError> GetKnownErrors()
    {
        return typeof(DomainErrors)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(type => type.GetFields(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly))
            .Where(field => field.FieldType == typeof(DomainError))
            .Select(field => Assert.IsType<DomainError>(field.GetValue(null)))
            .OrderBy(error => error.Code, StringComparer.Ordinal)
            .ToArray();
    }
}
