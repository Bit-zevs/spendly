using Spendly.Domain.Errors;

namespace Spendly.UnitTests.Domain.Errors;

public sealed class DomainErrorTests
{
    [Fact]
    public void Constructor_ShouldCreateDomainError_WhenCodeAndMessageAreProvided()
    {
        var error = new DomainError(
            "Wallet.Name.Empty",
            "Wallet name cannot be empty.");

        Assert.Equal("Wallet.Name.Empty", error.Code);
        Assert.Equal("Wallet name cannot be empty.", error.Message);
    }

    [Fact]
    public void ToString_ShouldReturnCodeAndMessage()
    {
        var error = new DomainError(
            "Wallet.Name.Empty",
            "Wallet name cannot be empty.");

        Assert.Equal(
            "Wallet.Name.Empty: Wallet name cannot be empty.",
            error.ToString());
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenErrorsHaveSameCodeAndMessage()
    {
        var first = new DomainError(
            "Wallet.Name.Empty",
            "Wallet name cannot be empty.");

        var second = new DomainError(
            "Wallet.Name.Empty",
            "Wallet name cannot be empty.");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_ShouldThrow_WhenCodeIsEmpty(string? code)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new DomainError(code!, "Domain error message."));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_ShouldThrow_WhenMessageIsEmpty(string? message)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new DomainError("Domain.Error", message!));

        Assert.Equal("message", exception.ParamName);
    }
}
