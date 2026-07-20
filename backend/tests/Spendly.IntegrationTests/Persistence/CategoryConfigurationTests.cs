using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Spendly.Domain.Categories;
using Spendly.Infrastructure.Persistence;

namespace Spendly.IntegrationTests.Persistence;

public sealed class CategoryConfigurationTests
{
    [Fact]
    public void Mapping_ShouldUseApprovedPostgreSqlContract()
    {
        using var context = CreateContext();

        var model = context.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(Category));

        Assert.NotNull(entityType);
        Assert.Equal("categories", entityType.GetTableName());
        Assert.Equal("pk_categories", entityType.FindPrimaryKey()?.GetName());

        var table = StoreObjectIdentifier.Table(
            entityType.GetTableName()!,
            entityType.GetSchema());

        AssertProperty(
            entityType,
            table,
            nameof(Category.Id),
            "id",
            "uuid",
            typeof(Guid));

        var idProperty = entityType.FindProperty(nameof(Category.Id));

        Assert.NotNull(idProperty);
        Assert.Equal(ValueGenerated.Never, idProperty.ValueGenerated);

        AssertProperty(
            entityType,
            table,
            nameof(Category.Name),
            "name");

        var nameProperty = entityType.FindProperty(nameof(Category.Name));

        Assert.NotNull(nameProperty);
        Assert.Equal(Category.MaxNameLength, nameProperty.GetMaxLength());

        AssertProperty(
            entityType,
            table,
            nameof(Category.Type),
            "type",
            "smallint",
            typeof(short));

        AssertProperty(
            entityType,
            table,
            nameof(Category.CreatedAt),
            "created_at",
            "timestamp with time zone");

        AssertCheckConstraint(
            entityType,
            "ck_categories_type_defined",
            "type IN (1, 2)");
    }

    [Fact]
    public void Mapping_ShouldMatchCurrentDomainShape()
    {
        using var context = CreateContext();

        var model = context.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(Category));

        Assert.NotNull(entityType);

        var propertyNames = entityType
            .GetProperties()
            .Select(property => property.Name)
            .Order()
            .ToArray();

        string[] expectedPropertyNames =
        [
            nameof(Category.CreatedAt),
            nameof(Category.Id),
            nameof(Category.Name),
            nameof(Category.Type)
        ];

        Assert.Equal(expectedPropertyNames, propertyNames);
        Assert.Empty(entityType.GetForeignKeys());
        Assert.Empty(entityType.GetIndexes());
        Assert.Empty(entityType.GetNavigations());
    }

    private static void AssertProperty(
        IEntityType entityType,
        StoreObjectIdentifier table,
        string propertyName,
        string columnName,
        string? columnType = null,
        Type? providerClrType = null)
    {
        var property = entityType.FindProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(columnName, property.GetColumnName(table));
        Assert.False(property.IsNullable);

        if (columnType is not null)
        {
            Assert.Equal(columnType, property.GetColumnType());
        }

        if (providerClrType is not null)
        {
            Assert.Equal(
                providerClrType,
                property.GetValueConverter()?.ProviderClrType);
        }
    }

    private static void AssertCheckConstraint(
        IEntityType entityType,
        string name,
        string sql)
    {
        var checkConstraint = entityType
            .GetCheckConstraints()
            .Single(constraint => constraint.Name == name);

        Assert.Equal(sql, checkConstraint.Sql);
    }

    private static CategoryConfigurationTestDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<CategoryConfigurationTestDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=spendly_category_mapping_tests;" +
                    "Username=spendly;Password=spendly")
                .Options;

        return new CategoryConfigurationTestDbContext(options);
    }

    private sealed class CategoryConfigurationTestDbContext(
        DbContextOptions<CategoryConfigurationTestDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(SpendlyDbContext).Assembly);
        }
    }
}
