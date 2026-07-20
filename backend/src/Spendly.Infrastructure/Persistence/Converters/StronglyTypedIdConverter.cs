using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Spendly.Domain.Common;

namespace Spendly.Infrastructure.Persistence.Converters;

internal abstract class StronglyTypedIdConverter<TId>
    : ValueConverter<TId, Guid>
    where TId : struct, IStronglyTypedId<Guid>
{
    protected StronglyTypedIdConverter(
        Expression<Func<Guid, TId>> convertFromProviderExpression)
        : base(
            id => GetProviderValue(id),
            convertFromProviderExpression)
    {
    }

    private static Guid GetProviderValue(TId id)
    {
        if (id.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"{typeof(TId).Name} cannot be persisted with an empty value.");
        }

        return id.Value;
    }
}
