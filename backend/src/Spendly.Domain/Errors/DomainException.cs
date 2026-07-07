namespace Spendly.Domain.Errors;

/// <summary>
/// Represents an exception caused by a domain business rule violation.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(DomainError error)
        : this(error, innerException: null)
    {
    }

    public DomainException(DomainError error, Exception? innerException)
        : base(EnsureNotNull(error).Message, innerException)
    {
        Error = error;
    }

    /// <summary>
    /// Gets the domain error that caused this exception.
    /// </summary>
    public DomainError Error { get; }

    /// <summary>
    /// Gets the stable machine-readable domain error code.
    /// </summary>
    public string Code => Error.Code;

    private static DomainError EnsureNotNull(DomainError error)
    {
        return error ?? throw new ArgumentNullException(nameof(error));
    }
}
