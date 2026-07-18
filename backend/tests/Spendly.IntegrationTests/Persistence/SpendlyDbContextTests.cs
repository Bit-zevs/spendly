using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.Wallets;
using Spendly.Infrastructure.Persistence;
using Spendly.Infrastructure.Persistence.DesignTime;

namespace Spendly.IntegrationTests.Persistence;

public sealed class SpendlyDbContextTests
{
    [Fact]
    public void Context_ShouldExposeExpectedDbSets()
    {
        AssertDbSet<Wallet>(nameof(SpendlyDbContext.Wallets));
        AssertDbSet<Category>(nameof(SpendlyDbContext.Categories));
        AssertDbSet<Transaction>(nameof(SpendlyDbContext.Transactions));
    }

    [Fact]
    public void DesignTimeFactory_ShouldCreateSpendlyDbContext()
    {
        var factory = new SpendlyDbContextFactory();

        using var context = factory.CreateDbContext([]);

        Assert.IsType<SpendlyDbContext>(context);
    }

    [Fact]
    public void Context_ShouldNotDeclareRepositoryMethods()
    {
        var declaredPublicMethods = typeof(SpendlyDbContext)
            .GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Empty(declaredPublicMethods);
    }

    private static void AssertDbSet<TEntity>(string propertyName)
        where TEntity : class
    {
        var property = typeof(SpendlyDbContext)
            .GetProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(typeof(DbSet<TEntity>), property.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);
    }
}
