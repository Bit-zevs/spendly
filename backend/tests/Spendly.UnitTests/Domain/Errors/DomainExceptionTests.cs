using Spendly.Domain.Errors;

namespace Spendly.UnitTests.Domain.Errors;

public sealed class DomainExceptionTests
{
    [Fact]
    public void Constructor_ShouldStoreDomainError()
    {
        var error = DomainErrors.Wallet.NameIsEmpty;

        var exception = new DomainException(error);

        Assert.Equal(error, exception.Error);
    }

    [Fact]
    public void Constructor_ShouldUseDomainErrorMessageAsExceptionMessage()
    {
        var error = DomainErrors.Wallet.NameIsEmpty;

        var exception = new DomainException(error);

        Assert.Equal(error.Message, exception.Message);
    }

    [Fact]
    public void Code_ShouldReturnDomainErrorCode()
    {
        var error = DomainErrors.Wallet.NameIsEmpty;

        var exception = new DomainException(error);

        Assert.Equal(error.Code, exception.Code);
    }

    [Fact]
    public void Constructor_ShouldPreserveInnerException()
    {
        var error = DomainErrors.Wallet.NameIsEmpty;
        var innerException = new InvalidOperationException("Inner exception.");

        var exception = new DomainException(error, innerException);

        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenErrorIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new DomainException(null!));

        Assert.Equal("error", exception.ParamName);
    }
}
