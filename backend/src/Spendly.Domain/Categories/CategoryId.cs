using Spendly.Domain.Common;

namespace Spendly.Domain.Categories;

public readonly record struct CategoryId : IStronglyTypedId<Guid>
{
    public CategoryId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Category id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static CategoryId New()
    {
        return new CategoryId(Guid.CreateVersion7());
    }

    public static CategoryId From(Guid value)
    {
        return new CategoryId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
