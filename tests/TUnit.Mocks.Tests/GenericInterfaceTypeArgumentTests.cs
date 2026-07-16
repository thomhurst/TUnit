using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/5403
/// Generic interfaces with non-built-in type arguments (enums, classes, nested namespaces,
/// multiple type arguments, nested generics) must produce valid generated class names.
/// </summary>
public class GenericInterfaceTypeArgumentTests
{
    public enum Priority
    {
        Low,
        Medium,
        High
    }

    public class ItemConfig
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    public interface IValueHolder<T>
    {
        T Value { get; }
        void SetValue(T value);
    }

    public interface IMapper<TIn, TOut>
    {
        TOut Map(TIn input);
    }

    public interface IProvider<T>
    {
        T Get();
        List<T> GetAll();
    }

    [Test]
    public async Task Generic_Interface_With_Enum_Type_Argument()
    {
        var mock = IValueHolder<Priority>.Mock();
        mock.Value.Returns(Priority.High);

        IValueHolder<Priority> holder = mock.Object;
        var result = holder.Value;

        await Assert.That(result).IsEqualTo(Priority.High);
    }

    [Test]
    public async Task Generic_Interface_With_Enum_Via_Static_Extension()
    {
        var mock = IValueHolder<Priority>.Mock();
        mock.Value.Returns(Priority.Medium);

        IValueHolder<Priority> holder = mock.Object;
        var result = holder.Value;

        await Assert.That(result).IsEqualTo(Priority.Medium);
    }

    [Test]
    public async Task Generic_Interface_With_Class_Type_Argument()
    {
        var mock = IValueHolder<ItemConfig>.Mock();
        var config = new ItemConfig { Name = "Test", Value = 42 };
        mock.Value.Returns(config);

        IValueHolder<ItemConfig> holder = mock.Object;
        var result = holder.Value;

        await Assert.That(result).IsSameReferenceAs(config);
        await Assert.That(result.Name).IsEqualTo("Test");
        await Assert.That(result.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Generic_Interface_With_Class_Via_Static_Extension()
    {
        var mock = IValueHolder<ItemConfig>.Mock();
        var config = new ItemConfig { Name = "Ext", Value = 99 };
        mock.Value.Returns(config);

        IValueHolder<ItemConfig> holder = mock.Object;
        var result = holder.Value;

        await Assert.That(result.Name).IsEqualTo("Ext");
    }

    [Test]
    public async Task Generic_Interface_With_Two_Non_Builtin_Type_Arguments()
    {
        var mock = IMapper<ItemConfig, Priority>.Mock();
        mock.Map(Any()).Returns(Priority.High);

        IMapper<ItemConfig, Priority> mapper = mock.Object;
        var result = mapper.Map(new ItemConfig { Name = "X", Value = 1 });

        await Assert.That(result).IsEqualTo(Priority.High);
    }

    [Test]
    public async Task Generic_Interface_With_Nested_Generic_Type_Argument()
    {
        var mock = IProvider<List<ItemConfig>>.Mock();
        var items = new List<ItemConfig> { new() { Name = "A", Value = 1 } };
        mock.Get().Returns(items);

        IProvider<List<ItemConfig>> provider = mock.Object;
        var result = provider.Get();

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Name).IsEqualTo("A");
    }

    [Test]
    public void Generic_Interface_With_Enum_Void_Method_Does_Not_Throw()
    {
        var mock = IValueHolder<Priority>.Mock();

        IValueHolder<Priority> holder = mock.Object;
        holder.SetValue(Priority.Low);
    }

    [Test]
    public void Generic_Interface_With_Enum_Verify_Calls()
    {
        var mock = IValueHolder<Priority>.Mock();

        IValueHolder<Priority> holder = mock.Object;
        holder.SetValue(Priority.High);
        holder.SetValue(Priority.Low);

        mock.SetValue(Any()).WasCalled(Times.Exactly(2));
        mock.SetValue(Priority.High).WasCalled(Times.Once);
        mock.SetValue(Priority.Low).WasCalled(Times.Once);
    }
}
