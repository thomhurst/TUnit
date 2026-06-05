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

    public interface IArityGreeter
    {
        string Greet3<T>(T obj, string a, string b) where T : class;
        string Greet4<T>(T obj, string a, string b, string c) where T : class;
        string Greet5<T>(T obj, string a, string b, string c, string d) where T : class;
        string Greet6<T>(T obj, string a, string b, string c, string d, string e) where T : class;
        string Greet7<T>(T obj, string a, string b, string c, string d, string e, string f) where T : class;
        string Greet8<T>(T obj, string a, string b, string c, string d, string e, string f, string g) where T : class;
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

    [Test]
    public async Task Greet_3_Params_Returns_Can_Access_All_Inputs()
    {
        var mock = IArityGreeter.Mock();

        mock.Greet3(Any<Class1>(), Any(), Any())
            .Returns((input1, input2, input3) => $"{input1.GetType().Name}:{input2}:{input3}");

        IArityGreeter greeter = mock;

        await Assert.That(greeter.Greet3(new Class1(), "a", "b")).IsEqualTo("Class1:a:b");
    }

    [Test]
    public async Task Greet_4_Params_Returns_Can_Access_All_Inputs()
    {
        var mock = IArityGreeter.Mock();

        mock.Greet4(Any<Class1>(), Any(), Any(), Any())
            .Returns((input1, input2, input3, input4) => $"{input1.GetType().Name}:{input2}:{input3}:{input4}");

        IArityGreeter greeter = mock;

        await Assert.That(greeter.Greet4(new Class1(), "a", "b", "c")).IsEqualTo("Class1:a:b:c");
    }

    [Test]
    public async Task Greet_5_Params_Returns_Can_Access_All_Inputs()
    {
        var mock = IArityGreeter.Mock();

        mock.Greet5(Any<Class1>(), Any(), Any(), Any(), Any())
            .Returns((input1, input2, input3, input4, input5) => $"{input1.GetType().Name}:{input2}:{input3}:{input4}:{input5}");

        IArityGreeter greeter = mock;

        await Assert.That(greeter.Greet5(new Class1(), "a", "b", "c", "d")).IsEqualTo("Class1:a:b:c:d");
    }

    [Test]
    public async Task Greet_6_Params_Returns_Can_Access_All_Inputs()
    {
        var mock = IArityGreeter.Mock();

        mock.Greet6(Any<Class1>(), Any(), Any(), Any(), Any(), Any())
            .Returns((input1, input2, input3, input4, input5, input6) => $"{input1.GetType().Name}:{input2}:{input3}:{input4}:{input5}:{input6}");

        IArityGreeter greeter = mock;

        await Assert.That(greeter.Greet6(new Class1(), "a", "b", "c", "d", "e")).IsEqualTo("Class1:a:b:c:d:e");
    }

    [Test]
    public async Task Greet_7_Params_Returns_Can_Access_All_Inputs()
    {
        var mock = IArityGreeter.Mock();

        mock.Greet7(Any<Class1>(), Any(), Any(), Any(), Any(), Any(), Any())
            .Returns((input1, input2, input3, input4, input5, input6, input7) => $"{input1.GetType().Name}:{input2}:{input3}:{input4}:{input5}:{input6}:{input7}");

        IArityGreeter greeter = mock;

        await Assert.That(greeter.Greet7(new Class1(), "a", "b", "c", "d", "e", "f")).IsEqualTo("Class1:a:b:c:d:e:f");
    }

    [Test]
    public async Task Greet_8_Params_Returns_Can_Access_All_Inputs()
    {
        var mock = IArityGreeter.Mock();

        mock.Greet8(Any<Class1>(), Any(), Any(), Any(), Any(), Any(), Any(), Any())
            .Returns((input1, input2, input3, input4, input5, input6, input7, input8) => $"{input1.GetType().Name}:{input2}:{input3}:{input4}:{input5}:{input6}:{input7}:{input8}");

        IArityGreeter greeter = mock;

        await Assert.That(greeter.Greet8(new Class1(), "a", "b", "c", "d", "e", "f", "g")).IsEqualTo("Class1:a:b:c:d:e:f:g");
    }
}
