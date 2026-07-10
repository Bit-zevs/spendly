namespace Spendly.Domain.Transactions;

/// <summary>
/// Defines the supported kinds of money movement represented by a transaction.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Money received from outside the user's tracked wallets.
    /// </summary>
    Income = 1,

    /// <summary>
    /// Money spent outside the user's tracked wallets.
    /// </summary>
    Expense = 2,

    /// <summary>
    /// Money moved between the user's tracked wallets without changing
    /// the user's total amount of tracked money.
    /// </summary>
    Transfer = 3
}
