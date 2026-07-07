using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.Wallets;

namespace Spendly.UnitTests.Domain.Common;

public sealed class StronglyTypedIdTests
{
    [Fact]
    public void WalletId_New_ShouldCreateNonEmptyVersion7Guid()
    {
        var id = WalletId.New();

        AssertVersion7Guid(id.Value);
    }

    [Fact]
    public void CategoryId_New_ShouldCreateNonEmptyVersion7Guid()
    {
        var id = CategoryId.New();

        AssertVersion7Guid(id.Value);
    }

    [Fact]
    public void TransactionId_New_ShouldCreateNonEmptyVersion7Guid()
    {
        var id = TransactionId.New();

        AssertVersion7Guid(id.Value);
    }

    [Fact]
    public void WalletId_From_ShouldPreserveProvidedGuid()
    {
        var value = Guid.CreateVersion7();

        var id = WalletId.From(value);

        Assert.Equal(value, id.Value);
    }

    [Fact]
    public void CategoryId_From_ShouldPreserveProvidedGuid()
    {
        var value = Guid.CreateVersion7();

        var id = CategoryId.From(value);

        Assert.Equal(value, id.Value);
    }

    [Fact]
    public void TransactionId_From_ShouldPreserveProvidedGuid()
    {
        var value = Guid.CreateVersion7();

        var id = TransactionId.From(value);

        Assert.Equal(value, id.Value);
    }

    [Fact]
    public void WalletId_From_ShouldThrow_WhenGuidIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() => WalletId.From(Guid.Empty));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void CategoryId_From_ShouldThrow_WhenGuidIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() => CategoryId.From(Guid.Empty));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void TransactionId_From_ShouldThrow_WhenGuidIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() => TransactionId.From(Guid.Empty));

        Assert.Equal("value", exception.ParamName);
    }

    private static void AssertVersion7Guid(Guid value)
    {
        Assert.NotEqual(Guid.Empty, value);
        Assert.Equal(7, value.Version);
    }
}
