using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Spendly.Infrastructure.Persistence.DesignTime;

public sealed class SpendlyDbContextFactory
    : IDesignTimeDbContextFactory<SpendlyDbContext>
{
    public SpendlyDbContext CreateDbContext(string[] _)
    {
        var options = new DbContextOptionsBuilder<SpendlyDbContext>()
            .UseNpgsql()
            .Options;

        return new SpendlyDbContext(options);
    }
}
