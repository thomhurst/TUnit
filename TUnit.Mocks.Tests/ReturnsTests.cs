namespace TUnit.Mocks.Tests;

public class ReturnsTests
{
    public interface IGreeter
    {
        string Greet(string name);
    }

    [Test]
    public async Task Greet_Returns_Configured_Value()
    {
        var mock = IGreeter.Mock();

        var nameArg = Any<string>();
        mock.Greet(nameArg).Returns(str => str);

        IGreeter greeter = mock;
        var result1 = greeter.Greet("Hello!");
        var result2 = greeter.Greet("Goodbye!");

        await Assert.That(result1).IsEqualTo("Hello!");
        await Assert.That(result2).IsEqualTo("Goodbye!");
    }

    public interface IGreeter2
    {
        string Greet<T>(T obj, string name) where T : class;
    }

    public class Class1
    {
    }

    public class Class2
    {
    }

    [Test]
    public async Task Greet_Multi_Param_Returns_Configured_Value()
    {
        var mock = IGreeter2.Mock();

        mock.Greet(Any<Class1>(), Any()).Returns((input1, input2) => input2);

        IGreeter2 greeter = mock;
        var obj1 = new Class1();
        var result1 = greeter.Greet(obj1, "Hello!");
        var result2 = greeter.Greet(obj1, "Goodbye!");

        await Assert.That(result1).IsEqualTo("Hello!");
        await Assert.That(result2).IsEqualTo("Goodbye!");
    }

    [Test]
    public async Task Greet_Multi_Param_Callback_Receives_All_Inputs()
    {
        var mock = IGreeter2.Mock();
        var callbackValues = new List<string>();

        mock.Greet(Any<Class1>(), Any())
            .Callback((input1, input2) => callbackValues.Add($"{input1.GetType().Name}:{input2}"))
            .Returns("ok");

        IGreeter2 greeter = mock;
        var result = greeter.Greet(new Class1(), "Hello!");

        await Assert.That(result).IsEqualTo("ok");
        await Assert.That(callbackValues).IsEquivalentTo(["Class1:Hello!"]);
    }

    [Test]
    public async Task Greet_Multi_Param_Throws_Receives_All_Inputs()
    {
        var mock = IGreeter2.Mock();

        mock.Greet(Any<Class1>(), Any())
            .Throws((input1, input2) => new InvalidOperationException($"{input1.GetType().Name}:{input2}"));

        IGreeter2 greeter = mock;
        var exception = Assert.Throws<InvalidOperationException>(() => greeter.Greet(new Class1(), "Hello!"));

        await Assert.That(exception!.Message).IsEqualTo("Class1:Hello!");
    }

    [Test]
    public void Greet_Multi_Param_Verification_Discriminates_Generic_Type_Arguments()
    {
        var mock = IGreeter2.Mock();
        IGreeter2 greeter = mock;

        greeter.Greet(new Class1(), "Hello!");
        greeter.Greet(new Class1(), "Hi!");
        greeter.Greet(new Class2(), "Goodbye!");

        mock.Greet<Class1>(Any(), Any()).WasCalled(Times.Exactly(2));
        mock.Greet<Class2>(Any(), Any()).WasCalled(Times.Once);
        mock.Greet<AnyType>(Any(), Any()).WasCalled(Times.Exactly(3));
    }
}
