using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Spendly.Domain.Categories;
using Spendly.IntegrationTests.Database;

namespace Spendly.IntegrationTests.Persistence;

[Collection<PostgreSqlDatabaseCollection>]
public sealed class CategoryPersistenceTests(
    PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database)
{
    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task Category_ShouldRoundTripWithoutDataLoss()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var sourceCreatedAt = CreateSourceCreatedAt();

        var category = Category.Create(
            name: "  Groceries  ",
            type: CategoryType.Expense,
            createdAt: sourceCreatedAt);

        AssertCategoryPersistenceShape();
        Assert.Equal(7, category.Id.Value.Version);
        Assert.Equal(sourceCreatedAt.ToUniversalTime(), category.CreatedAt);

        await using (var writeContext = Database.CreateDbContext())
        {
            writeContext.Categories.Add(category);

            await writeContext.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = Database.CreateDbContext();

        var restoredCategory = await readContext.Categories
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == category.Id,
                cancellationToken);

        Assert.NotSame(category, restoredCategory);
        Assert.Equal(category.Id, restoredCategory.Id);
        Assert.Equal(7, restoredCategory.Id.Value.Version);
        Assert.Equal("Groceries", restoredCategory.Name);
        Assert.Equal(category.Type, restoredCategory.Type);
        Assert.Equal(category.CreatedAt, restoredCategory.CreatedAt);
        Assert.Equal(TimeSpan.Zero, restoredCategory.CreatedAt.Offset);
        Assert.Empty(readContext.ChangeTracker.Entries());

        await AssertStoredUsingPostgreSqlContractAsync(
            category,
            cancellationToken);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task EveryDefinedCategoryType_ShouldRoundTrip()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var categoryTypes = Enum.GetValues<CategoryType>();

        var categories = categoryTypes
            .Select(
                (categoryType, index) => Category.Create(
                    name: $"Category {categoryType}",
                    type: categoryType,
                    createdAt: CreateUtcCreatedAt().AddMinutes(index)))
            .ToArray();

        await using (var writeContext = Database.CreateDbContext())
        {
            writeContext.Categories.AddRange(categories);

            await writeContext.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = Database.CreateDbContext();

        var restoredCategories = await readContext.Categories
            .AsNoTracking()
            .ToDictionaryAsync(
                category => category.Id,
                cancellationToken);

        Assert.Equal(categories.Length, restoredCategories.Count);

        foreach (var category in categories)
        {
            var restoredCategory = restoredCategories[category.Id];

            Assert.Equal(category.Type, restoredCategory.Type);
        }

        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task CategoryName_AtMaximumLength_ShouldRoundTrip()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var maximumLengthName = new string('C', Category.MaxNameLength);

        var category = Category.Create(
            name: maximumLengthName,
            type: CategoryType.Income,
            createdAt: CreateUtcCreatedAt());

        await using (var writeContext = Database.CreateDbContext())
        {
            writeContext.Categories.Add(category);

            await writeContext.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = Database.CreateDbContext();

        var restoredName = await readContext.Categories
            .AsNoTracking()
            .Where(candidate => candidate.Id == category.Id)
            .Select(candidate => candidate.Name)
            .SingleAsync(cancellationToken);

        Assert.Equal(maximumLengthName, restoredName);
        Assert.Equal(Category.MaxNameLength, restoredName.Length);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task CategoryName_ExceedingMaximumLength_ShouldBeRejectedByPostgreSql()
    {
        var exception = await AssertInvalidCategoryIsRejectedAsync(
            name: new string('C', Category.MaxNameLength + 1),
            type: (short)CategoryType.Expense);

        Assert.Equal(
            PostgresErrorCodes.StringDataRightTruncation,
            exception.SqlState);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task CategoryName_Null_ShouldBeRejectedByPostgreSql()
    {
        var exception = await AssertInvalidCategoryIsRejectedAsync(
            name: null,
            type: (short)CategoryType.Expense);

        Assert.Equal(
            PostgresErrorCodes.NotNullViolation,
            exception.SqlState);

        Assert.Equal("name", exception.ColumnName);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task UndefinedCategoryType_ShouldBeRejectedByPostgreSql()
    {
        var exception = await AssertInvalidCategoryIsRejectedAsync(
            name: "Invalid type category",
            type: 0);

        Assert.Equal(
            PostgresErrorCodes.CheckViolation,
            exception.SqlState);

        Assert.Equal(
            "ck_categories_type_defined",
            exception.ConstraintName);
    }

    private async Task<PostgresException> AssertInvalidCategoryIsRejectedAsync(
        string? name,
        short type)
    {
        const string sql =
            """
            INSERT INTO categories (
                id,
                name,
                type,
                created_at)
            VALUES (
                @category_id,
                @name,
                @type,
                @created_at);
            """;

        var cancellationToken = TestContext.Current.CancellationToken;

        await using var dataSource =
            NpgsqlDataSource.Create(Database.ConnectionString);

        await using var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(
            "category_id",
            NpgsqlDbType.Uuid,
            Guid.CreateVersion7());

        var nameParameter = command.Parameters.Add(
            "name",
            NpgsqlDbType.Varchar);

        nameParameter.Value = (object?)name ?? DBNull.Value;

        command.Parameters.AddWithValue(
            "type",
            NpgsqlDbType.Smallint,
            type);

        command.Parameters.AddWithValue(
            "created_at",
            NpgsqlDbType.TimestampTz,
            CreateUtcCreatedAt());

        return await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync(cancellationToken));
    }

    private async Task AssertStoredUsingPostgreSqlContractAsync(
        Category category,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT
                id,
                pg_typeof(id)::text,
                type,
                pg_typeof(type)::text,
                created_at,
                pg_typeof(created_at)::text
            FROM categories
            WHERE id = @category_id;
            """;

        await using var dataSource =
            NpgsqlDataSource.Create(Database.ConnectionString);

        await using var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(
            "category_id",
            NpgsqlDbType.Uuid,
            category.Id.Value);

        await using var reader = await command.ExecuteReaderAsync(
            cancellationToken);

        Assert.True(await reader.ReadAsync(cancellationToken));
        Assert.Equal(category.Id.Value, reader.GetGuid(0));
        Assert.Equal("uuid", reader.GetString(1));
        Assert.Equal((short)category.Type, reader.GetInt16(2));
        Assert.Equal("smallint", reader.GetString(3));

        var storedCreatedAt = reader.GetFieldValue<DateTimeOffset>(4);

        Assert.Equal(category.CreatedAt, storedCreatedAt);
        Assert.Equal(TimeSpan.Zero, storedCreatedAt.Offset);
        Assert.Equal("timestamp with time zone", reader.GetString(5));
        Assert.False(await reader.ReadAsync(cancellationToken));
    }

    private static void AssertCategoryPersistenceShape()
    {
        string[] propertyNames =
        [
            nameof(Category.Id),
            nameof(Category.Name),
            nameof(Category.Type),
            nameof(Category.CreatedAt)
        ];

        foreach (var propertyName in propertyNames)
        {
            var property = typeof(Category).GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);

            Assert.NotNull(property);
            Assert.False(
                property.SetMethod?.IsPublic is true,
                $"{nameof(Category)}.{propertyName} must not expose a public setter.");
        }

        var constructor = typeof(Category).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types:
            [
                typeof(CategoryId),
                typeof(string),
                typeof(CategoryType),
                typeof(DateTimeOffset)
            ],
            modifiers: null);

        Assert.NotNull(constructor);
        Assert.True(constructor.IsPrivate);
    }

    private static DateTimeOffset CreateSourceCreatedAt()
    {
        return new DateTimeOffset(
            2026,
            7,
            27,
            12,
            34,
            56,
            789,
            TimeSpan.FromHours(5));
    }

    private static DateTimeOffset CreateUtcCreatedAt()
    {
        return CreateSourceCreatedAt().ToUniversalTime();
    }
}
