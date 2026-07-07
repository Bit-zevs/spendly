using Spendly.Domain.Common;

namespace Spendly.UnitTests.Domain.Common;

public sealed class ValueObjectTests
{
    [Fact]
    public void Equals_ShouldReturnTrue_WhenValueObjectsHaveSameConcreteTypeAndComponents()
    {
        var first = new TestValueObject("Food", 100);
        var second = new TestValueObject("Food", 100);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenValueObjectsHaveDifferentComponents()
    {
        var first = new TestValueObject("Food", 100);
        var second = new TestValueObject("Transport", 100);

        Assert.NotEqual(first, second);
        Assert.False(first == second);
        Assert.True(first != second);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenValueObjectsHaveDifferentConcreteTypes()
    {
        ValueObject first = new TestValueObject("Food", 100);
        ValueObject second = new OtherTestValueObject("Food", 100);

        Assert.NotEqual(first, second);
        Assert.False(first == second);
        Assert.True(first != second);
    }

    private sealed class TestValueObject(string name, int amount) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return name;
            yield return amount;
        }
    }

    private sealed class OtherTestValueObject(string name, int amount) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return name;
            yield return amount;
        }
    }
}
