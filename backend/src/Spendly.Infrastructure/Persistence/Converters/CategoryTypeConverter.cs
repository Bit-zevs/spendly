using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Spendly.Domain.Categories;

namespace Spendly.Infrastructure.Persistence.Converters;

internal sealed class CategoryTypeConverter
    : ValueConverter<CategoryType, short>
{
    public CategoryTypeConverter()
        : base(
            categoryType => (short)categoryType,
            value => (CategoryType)value)
    {
    }
}
