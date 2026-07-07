using Spendly.Domain.Common;

namespace Spendly.UnitTests.Domain.Common;

public sealed class EntityTests
{
    [Fact]
    public void Equals_ShouldReturnTrue_WhenEntitiesHaveSameConcreteTypeAndId()
    {
        var id = TestEntityId.New();

        var first = new TestEntity(id);
        var second = new TestEntity(id);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenEntitiesHaveDifferentIds()
    {
        var first = new TestEntity(TestEntityId.New());
        var second = new TestEntity(TestEntityId.New());

        Assert.NotEqual(first, second);
        Assert.False(first == second);
        Assert.True(first != second);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenEntitiesHaveDifferentConcreteTypes()
    {
        var id = TestEntityId.New();

        Entity<TestEntityId> first = new TestEntity(id);
        Entity<TestEntityId> second = new OtherTestEntity(id);

        Assert.NotEqual(first, second);
        Assert.False(first == second);
        Assert.True(first != second);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenIdHasDefaultValue()
    {
        var exception = Assert.Throws<ArgumentException>(() => new TestEntity(default));

        Assert.Equal("id", exception.ParamName);
    }

    private sealed class TestEntity(TestEntityId id) : Entity<TestEntityId>(id);

    private sealed class OtherTestEntity(TestEntityId id) : Entity<TestEntityId>(id);

    private readonly record struct TestEntityId(Guid Value) : IStronglyTypedId<Guid>
    {
        public static TestEntityId New()
        {
            return new TestEntityId(Guid.CreateVersion7());
        }
    }
}
