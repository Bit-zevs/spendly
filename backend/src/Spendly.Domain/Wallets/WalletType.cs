namespace Spendly.Domain.Wallets;

/// <summary>
/// Defines the supported kinds of wallets.
/// </summary>
public enum WalletType
{
    /// <summary>
    /// Physical cash kept outside a financial institution.
    /// </summary>
    Cash = 1,

    /// <summary>
    /// A payment card backed by the owner's own funds.
    /// </summary>
    DebitCard = 2,

    /// <summary>
    /// A payment card backed by borrowed funds.
    /// </summary>
    CreditCard = 3,

    /// <summary>
    /// A general-purpose bank account.
    /// </summary>
    BankAccount = 4,

    /// <summary>
    /// An account primarily intended for saving money.
    /// </summary>
    Savings = 5,

    /// <summary>
    /// An investment account or portfolio.
    /// </summary>
    Investment = 6,

    /// <summary>
    /// Another supported kind of wallet that does not fit the known categories.
    /// </summary>
    Other = 7
}
