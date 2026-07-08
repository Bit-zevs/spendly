using Spendly.Domain.Errors;

namespace Spendly.UnitTests.TestUtilities;

internal static class DomainExceptionAssert
{
    public static DomainException Throws(DomainError expectedError, Action action)
    {
        ArgumentNullException.ThrowIfNull(expectedError);
        ArgumentNullException.ThrowIfNull(action);

        var exception = Assert.Throws<DomainException>(action);

        Assert.Equal(expectedError, exception.Error);
        Assert.Equal(expectedError.Code, exception.Code);
        Assert.Equal(expectedError.Message, exception.Message);

        return exception;
    }
}
