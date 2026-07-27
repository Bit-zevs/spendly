using Microsoft.EntityFrameworkCore;
using Spendly.Infrastructure.Persistence.DesignTime;

namespace Spendly.IntegrationTests.Persistence;

public sealed class InitialDatabaseMigrationModelTests
{
    [Fact]
    public void Model_ShouldMatchInitialMigration()
    {
        var factory = new SpendlyDbContextFactory();

        using var context = factory.CreateDbContext([]);

        var migration = Assert.Single(context.Database.GetMigrations());

        Assert.EndsWith(
            "_InitialCreate",
            migration,
            StringComparison.Ordinal);

        Assert.False(context.Database.HasPendingModelChanges());
    }
}
