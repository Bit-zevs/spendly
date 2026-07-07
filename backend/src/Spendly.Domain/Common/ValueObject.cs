namespace Spendly.Domain.Common;

/// <summary>
/// Base type for immutable domain objects that are identified by their values.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }

    public bool Equals(ValueObject? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType()
               && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj)
    {
        return obj is ValueObject valueObject && Equals(valueObject);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        hashCode.Add(GetType());

        foreach (var component in GetEqualityComponents())
        {
            hashCode.Add(component);
        }

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Gets the components that define value object equality.
    /// </summary>
    /// <returns>The ordered equality components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();
}
