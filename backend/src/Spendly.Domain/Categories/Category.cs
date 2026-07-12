using Spendly.Domain.Common;
using Spendly.Domain.Errors;

namespace Spendly.Domain.Categories;

public sealed class Category : Entity<CategoryId>
{
    public const int MaxNameLength = 100;

    private Category(
        CategoryId id,
        string name,
        CategoryType type,
        DateTimeOffset createdAt)
        : base(id)
    {
        Name = name;
        Type = type;
        CreatedAt = createdAt;
    }

    public string Name { get; }

    public CategoryType Type { get; }

    public DateTimeOffset CreatedAt { get; }

    public static Category Create(
        string? name,
        CategoryType type,
        DateTimeOffset createdAt)
    {
        var normalizedName = NormalizeName(name);

        EnsureTypeIsValid(type);

        var utcCreatedAt = NormalizeCreatedAt(createdAt);

        return new Category(
            CategoryId.New(),
            normalizedName,
            type,
            utcCreatedAt);
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(DomainErrors.Category.NameIsEmpty);
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new DomainException(DomainErrors.Category.NameIsTooLong);
        }

        return normalizedName;
    }

    private static void EnsureTypeIsValid(CategoryType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException(DomainErrors.Category.TypeIsInvalid);
        }
    }

    private static DateTimeOffset NormalizeCreatedAt(DateTimeOffset createdAt)
    {
        if (createdAt == default)
        {
            throw new DomainException(DomainErrors.Category.CreatedAtIsInvalid);
        }

        return createdAt.ToUniversalTime();
    }
}
