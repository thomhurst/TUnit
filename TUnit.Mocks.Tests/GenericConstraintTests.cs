using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Interfaces with various generic method constraints that previously caused CS0460
/// when the source generator copied non-class/struct constraints to explicit interface implementations.
/// </summary>
public interface INotnullService
{
    T Get<T>(string key) where T : notnull;
}

public interface INewConstraintService
{
    T Create<T>() where T : new();
}

public interface IBaseTypeConstraintService
{
    T Get<T>() where T : IDisposable;
}

public interface ICompositeConstraintService
{
    T Create<T>() where T : class, IDisposable, new();
    T Read<T>() where T : struct, IComparable<T>;
}

public class GenericConstraintTests
{
    [Test]
    public async Task Notnull_Constraint_Mock_Returns_Configured_Value()
    {
        var mock = Mock.Of<INotnullService>();
        mock.Get<int>("key").Returns(42);

        INotnullService svc = mock.Object;
        var result = svc.Get<int>("key");

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public void New_Constraint_Mock_Does_Not_Throw()
    {
        var mock = Mock.Of<INewConstraintService>();

        INewConstraintService svc = mock.Object;
        _ = svc.Create<object>();
    }

    [Test]
    public void Base_Type_Constraint_Mock_Does_Not_Throw()
    {
        var mock = Mock.Of<IBaseTypeConstraintService>();

        IBaseTypeConstraintService svc = mock.Object;
        _ = svc.Get<MemoryStream>();
    }

    [Test]
    public void Composite_Class_Constraint_Mock_Does_Not_Throw()
    {
        var mock = Mock.Of<ICompositeConstraintService>();

        ICompositeConstraintService svc = mock.Object;
        _ = svc.Create<MemoryStream>();
    }

    [Test]
    public async Task Composite_Struct_Constraint_Mock_Returns_Default()
    {
        var mock = Mock.Of<ICompositeConstraintService>();

        ICompositeConstraintService svc = mock.Object;
        var result = svc.Read<int>();

        await Assert.That(result).IsEqualTo(0);
    }
}
