namespace Spendly.Domain.Errors;

/// <summary>
/// Describes a domain-level business rule violation without any transport-specific details.
/// </summary>
public sealed record DomainError
{
    public DomainError(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Domain error code cannot be empty.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Domain error message cannot be empty.", nameof(message));
        }

        Code = code;
        Message = message;
    }

    /// <summary>
    /// Gets the stable machine-readable error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public string Message { get; }

    public override string ToString()
    {
        return $"{Code}: {Message}";
    }
}
