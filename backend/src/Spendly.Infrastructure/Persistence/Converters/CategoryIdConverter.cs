using Spendly.Domain.Categories;

namespace Spendly.Infrastructure.Persistence.Converters;

internal sealed class CategoryIdConverter
    : StronglyTypedIdConverter<CategoryId>
{
    public CategoryIdConverter()
        : base(value => CategoryId.From(value))
    {
    }
}
