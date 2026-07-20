using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.Wallets;
using Spendly.Infrastructure.Persistence.Converters;

namespace Spendly.IntegrationTests.Persistence;

public sealed class StronglyTypedIdConverterTests
{
    [Fact]
    public void WalletIdConverter_ShouldRoundTripGuid()
    {
        AssertRoundTrip(
            new WalletIdConverter(),
            WalletId.From);
    }

    [Fact]
    public void CategoryIdConverter_ShouldRoundTripGuid()
    {
        AssertRoundTrip(
            new CategoryIdConverter(),
            CategoryId.From);
    }

    [Fact]
    public void TransactionIdConverter_ShouldRoundTripGuid()
    {
        AssertRoundTrip(
            new TransactionIdConverter(),
            TransactionId.From);
    }

    [Fact]
    public void WalletIdConverter_ShouldRejectDefaultModelValue()
    {
        AssertDefaultModelValueRejected(new WalletIdConverter());
    }

    [Fact]
    public void CategoryIdConverter_ShouldRejectDefaultModelValue()
    {
        AssertDefaultModelValueRejected(new CategoryIdConverter());
    }

    [Fact]
    public void TransactionIdConverter_ShouldRejectDefaultModelValue()
    {
        AssertDefaultModelValueRejected(new TransactionIdConverter());
    }

    [Fact]
    public void WalletIdConverter_ShouldRejectEmptyProviderValue()
    {
        AssertEmptyProviderValueRejected(new WalletIdConverter());
    }

    [Fact]
    public void CategoryIdConverter_ShouldRejectEmptyProviderValue()
    {
        AssertEmptyProviderValueRejected(new CategoryIdConverter());
    }

    [Fact]
    public void TransactionIdConverter_ShouldRejectEmptyProviderValue()
    {
        AssertEmptyProviderValueRejected(new TransactionIdConverter());
    }

    [Fact]
    public void Query_ShouldTranslateStronglyTypedIdParameter()
    {
        using var context = CreateContext();

        var id = WalletId.New();

        var sql = context
            .Set<WalletIdProbe>()
            .Where(probe => probe.Id == id)
            .ToQueryString();

        Assert.NotEmpty(sql);
    }

    [Fact]
    public void ChangeTracker_ShouldTreatEquivalentIdsAsUnchanged()
    {
        using var context = CreateContext();

        var value = Guid.CreateVersion7();
        var entity = new WalletIdProbe
        {
            Id = WalletId.From(value)
        };

        context.Attach(entity);

        entity.Id = WalletId.From(value);
        context.ChangeTracker.DetectChanges();

        var entry = context.Entry(entity);
        var idProperty = entry.Property(probe => probe.Id);

        Assert.Equal(EntityState.Unchanged, entry.State);
        Assert.False(idProperty.IsModified);
    }

    [Fact]
    public void ChangeTracker_ShouldRejectDuplicateEquivalentKeys()
    {
        using var context = CreateContext();

        var id = WalletId.New();

        context.Attach(new WalletIdProbe { Id = id });

        Assert.Throws<InvalidOperationException>(
            () => context.Attach(new WalletIdProbe { Id = id }));
    }

    [Fact]
    public void ChangeTracker_ShouldKeepDifferentIdTypesSeparated()
    {
        using var context = CreateContext();

        var value = Guid.CreateVersion7();

        context.Attach(
            new WalletIdProbe
            {
                Id = WalletId.From(value)
            });

        context.Attach(
            new CategoryIdProbe
            {
                Id = CategoryId.From(value)
            });

        context.Attach(
            new TransactionIdProbe
            {
                Id = TransactionId.From(value)
            });

        Assert.Single(context.ChangeTracker.Entries<WalletIdProbe>());
        Assert.Single(context.ChangeTracker.Entries<CategoryIdProbe>());
        Assert.Single(context.ChangeTracker.Entries<TransactionIdProbe>());
    }

    private static void AssertRoundTrip<TId>(
        ValueConverter<TId, Guid> converter,
        Func<Guid, TId> createId)
    {
        var value = Guid.CreateVersion7();
        var expectedId = createId(value);

        var convertToProvider =
            converter.ConvertToProviderExpression.Compile();
        var convertFromProvider =
            converter.ConvertFromProviderExpression.Compile();

        var providerValue = convertToProvider(expectedId);
        var actualId = convertFromProvider(providerValue);

        Assert.Equal(value, providerValue);
        Assert.Equal(expectedId, actualId);
    }

    private static void AssertDefaultModelValueRejected<TId>(
        ValueConverter<TId, Guid> converter)
        where TId : struct
    {
        var convertToProvider =
            converter.ConvertToProviderExpression.Compile();

        var exception = Assert.Throws<InvalidOperationException>(
            () => convertToProvider(default));

        Assert.Contains(typeof(TId).Name, exception.Message);
    }

    private static void AssertEmptyProviderValueRejected<TId>(
        ValueConverter<TId, Guid> converter)
    {
        var convertFromProvider =
            converter.ConvertFromProviderExpression.Compile();

        var exception = Assert.Throws<ArgumentException>(
            () => convertFromProvider(Guid.Empty));

        Assert.Equal("value", exception.ParamName);
    }

    private static ConverterTestDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<ConverterTestDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=spendly_converter_tests;" +
                    "Username=spendly;Password=spendly")
                .Options;

        return new ConverterTestDbContext(options);
    }

    private sealed class ConverterTestDbContext(
        DbContextOptions<ConverterTestDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WalletIdProbe>(builder =>
            {
                builder.HasKey(probe => probe.Id);

                builder
                    .Property(probe => probe.Id)
                    .HasConversion(new WalletIdConverter())
                    .ValueGeneratedNever();
            });

            modelBuilder.Entity<CategoryIdProbe>(builder =>
            {
                builder.HasKey(probe => probe.Id);

                builder
                    .Property(probe => probe.Id)
                    .HasConversion(new CategoryIdConverter())
                    .ValueGeneratedNever();
            });

            modelBuilder.Entity<TransactionIdProbe>(builder =>
            {
                builder.HasKey(probe => probe.Id);

                builder
                    .Property(probe => probe.Id)
                    .HasConversion(new TransactionIdConverter())
                    .ValueGeneratedNever();
            });
        }
    }

    private sealed class WalletIdProbe
    {
        public WalletId Id { get; set; }
    }

    private sealed class CategoryIdProbe
    {
        public CategoryId Id { get; set; }
    }

    private sealed class TransactionIdProbe
    {
        public TransactionId Id { get; set; }
    }
}
