using Microsoft.EntityFrameworkCore;
using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.Wallets;
using Spendly.IntegrationTests.Persistence.Compatibility.Configurations;

namespace Spendly.IntegrationTests.Persistence.Compatibility;

internal sealed class EfCoreCompatibilityDbContext(
    DbContextOptions<EfCoreCompatibilityDbContext> options)
    : DbContext(options)
{
    public DbSet<Wallet> Wallets => Set<Wallet>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(
            new WalletCompatibilityConfiguration());

        modelBuilder.ApplyConfiguration(
            new CategoryCompatibilityConfiguration());

        modelBuilder.ApplyConfiguration(
            new TransactionCompatibilityConfiguration());
    }
}
