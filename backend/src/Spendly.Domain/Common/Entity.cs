namespace Spendly.Domain.Common;

/// <summary>
/// Base type for domain entities that are identified by a stable identity.
/// </summary>
/// <typeparam name="TId">The strongly typed entity identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// Initializes a new entity instance with the specified identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <exception cref="ArgumentException">Thrown when the identifier has the default value.</exception>
    protected Entity(TId id)
    {
        if (EqualityComparer<TId>.Default.Equals(id, default!))
        {
            throw new ArgumentException("Entity id cannot be the default value.", nameof(id));
        }

        Id = id;
    }

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public TId Id { get; }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }

    public bool Equals(Entity<TId>? other)
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
               && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Equals(entity);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }
}
