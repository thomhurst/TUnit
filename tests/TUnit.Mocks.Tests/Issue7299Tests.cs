using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/7299
// A generic interface whose type parameter shares its name with a method type parameter (here
// inherited, as Wolverine's IDbContextOutbox<T> inherits IMessageBus.InvokeAsync<T>) generated
// mock call classes declaring both (`MockCall<T, T>`, CS0692) and extension blocks whose methods
// redeclared the block's type parameter (CS9289). Colliding method type parameters are renamed.

#region Test types

public interface IBus7299
{
    T Invoke<T>(object message);
}

public interface IOutbox7299<T> : IBus7299
{
    T Db { get; }
}

// The interface's own type parameter and the method's both appear in one signature.
public interface IConverter7299<TResult>
{
    TResult Convert<T>(T value);
}

public interface IStore7299<T> : IConverter7299<T>
{
    void Put<TKey>(TKey key, T value) where TKey : notnull;
}

// The renamed type parameter is used in a constraint of another method type parameter.
public interface IConstrained7299<T>
{
    TOther Select<T, TOther>(T value) where TOther : T;
}

// The default replacement name is taken by a value parameter of the method (CS0412).
public interface IParameterNamed7299<T>
{
    int Invoke<T>(int TMethod, T value);
}

// The default replacement name is the method's own name (CS0694).
public interface IMethodNamed7299<T>
{
    int TMethod<T>(T value);
}

// A mocked class's virtual generic method collides with the class's type parameter.
public class Handler7299<T>
{
    public virtual T Handle<T>(T input) => input;
}

#endregion

public class Issue7299Tests
{
    [Test]
    public async Task Issue_Repro_Compiles_And_Configures_The_Property()
    {
        var mock = Mock.Of<IOutbox7299<string>>();
        mock.Db.Returns("x");

        await Assert.That(mock.Object.Db).IsEqualTo("x");
    }

    [Test]
    public async Task Inherited_Generic_Method_With_Same_Type_Parameter_Name_Can_Be_Configured()
    {
        var mock = IOutbox7299<string>.Mock();
        mock.Db.Returns("db");
        mock.Invoke<int>(Arg.Any<object>()).Returns(42);

        await Assert.That(mock.Object.Db).IsEqualTo("db");
        await Assert.That(mock.Object.Invoke<int>("message")).IsEqualTo(42);
        await Assert.That(mock.Object.Invoke<string>("message")).IsNull();
    }

    [Test]
    public async Task Method_Type_Parameter_Is_Kept_Apart_From_The_Interface_Type_Parameter()
    {
        var mock = IStore7299<string>.Mock();
        mock.Convert<int>(5).Returns("five");

        IStore7299<string> store = mock.Object;
        store.Put(1, "one");
        store.Put("key", "two");

        await Assert.That(store.Convert(5)).IsEqualTo("five");
        await Assert.That(store.Convert("5")).IsNull();
        mock.Put<int>(1, "one").WasCalled(Times.Once);
        mock.Put<string>(Arg.Any<string>(), Arg.Any<string>()).WasCalled(Times.Once);
    }

    [Test]
    public async Task Renamed_Type_Parameter_Is_Renamed_In_Constraints()
    {
        var mock = IConstrained7299<int>.Mock();
        mock.Select<object, string>("value").Returns("selected");

        await Assert.That(mock.Object.Select<object, string>("value")).IsEqualTo("selected");
    }

    [Test]
    public async Task Renamed_Type_Parameter_Does_Not_Clash_With_A_Value_Parameter_Name()
    {
        var mock = IParameterNamed7299<string>.Mock();
        mock.Invoke<string>(1, "value").Returns(7);

        await Assert.That(mock.Object.Invoke(1, "value")).IsEqualTo(7);
        await Assert.That(mock.Object.Invoke(1, 5)).IsEqualTo(0);
    }

    [Test]
    public async Task Renamed_Type_Parameter_Does_Not_Clash_With_The_Method_Name()
    {
        var mock = IMethodNamed7299<string>.Mock();
        mock.TMethod<int>(4).Returns(16);

        await Assert.That(mock.Object.TMethod(4)).IsEqualTo(16);
        await Assert.That(mock.Object.TMethod("4")).IsEqualTo(0);
    }

    [Test]
    public async Task Class_Virtual_Generic_Method_With_Same_Type_Parameter_Name_Can_Be_Configured()
    {
        var mock = Handler7299<string>.Mock();
        mock.Handle<int>(3).Returns(9);

        await Assert.That(mock.Object.Handle(3)).IsEqualTo(9);
    }
}
