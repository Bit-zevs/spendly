namespace Spendly.Domain.Common;

/// <summary>
/// Represents a type-safe identifier that wraps an underlying primitive value.
/// </summary>
/// <typeparam name="TValue">The underlying identifier value type.</typeparam>
public interface IStronglyTypedId<out TValue>
    where TValue : notnull
{
    /// <summary>
    /// Gets the underlying identifier value.
    /// </summary>
    TValue Value { get; }
}
