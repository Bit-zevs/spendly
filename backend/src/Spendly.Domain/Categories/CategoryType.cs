namespace Spendly.Domain.Categories;

/// <summary>
/// Defines whether a category classifies incoming or outgoing money.
/// </summary>
public enum CategoryType
{
    /// <summary>
    /// A category used for money received by the user.
    /// </summary>
    Income = 1,

    /// <summary>
    /// A category used for money spent by the user.
    /// </summary>
    Expense = 2
}
