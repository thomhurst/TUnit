using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Test interfaces for the Arg.AnyArgs shortcut. The shortcut emits a single
/// setup overload that fills every matchable parameter with Any(), so a user
/// no longer has to write Any() once per parameter.
/// </summary>
public interface IFiveParamService
{
    int Compute(int a, int b, int c, int d, int e);
    void Log(string source, string level, string message, int code, bool fatal);
}

public interface ITwoParamService
{
    string Combine(string left, string right);
}

public interface IOverloadedSumService
{
    int Sum(int a, int b);
    int Sum(int a, int b, int c);
    void Notify(string message);
    void Notify(string source, string message);
}

public class AnyArgsTests
{
    [Test]
    public async Task AnyArgs_Setup_Returns_Value_For_Any_Argument_Combination()
    {
        var mock = IFiveParamService.Mock();
        mock.Compute(AnyArgs()).Returns(42);

        var svc = mock.Object;

        await Assert.That(svc.Compute(0, 0, 0, 0, 0)).IsEqualTo(42);
        await Assert.That(svc.Compute(1, 2, 3, 4, 5)).IsEqualTo(42);
        await Assert.That(svc.Compute(-100, int.MaxValue, 0, 7, -1)).IsEqualTo(42);
    }

    [Test]
    public async Task AnyArgs_Setup_Works_For_Two_Param_Method()
    {
        var mock = ITwoParamService.Mock();
        mock.Combine(AnyArgs()).Returns("OK");

        var svc = mock.Object;

        await Assert.That(svc.Combine("a", "b")).IsEqualTo("OK");
        await Assert.That(svc.Combine("", "")).IsEqualTo("OK");
        await Assert.That(svc.Combine(null!, null!)).IsEqualTo("OK");
    }

    [Test]
    public async Task AnyArgs_Setup_Works_For_Void_Method()
    {
        var mock = IFiveParamService.Mock();
        mock.Log(AnyArgs()).Throws(new System.InvalidOperationException("boom"));

        var svc = mock.Object;

        await Assert.That(() => svc.Log("a", "b", "c", 0, false))
            .Throws<System.InvalidOperationException>();
        await Assert.That(() => svc.Log("x", "y", "z", 99, true))
            .Throws<System.InvalidOperationException>();
    }

    [Test]
    public async Task AnyArgs_Verify_Catches_Any_Call()
    {
        var mock = IFiveParamService.Mock();
        var svc = mock.Object;

        svc.Compute(1, 2, 3, 4, 5);
        svc.Compute(10, 20, 30, 40, 50);

        mock.Compute(AnyArgs()).WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task AnyArgs_Equivalent_To_All_Any_Slots()
    {
        var mockA = IFiveParamService.Mock();
        var mockB = IFiveParamService.Mock();

        mockA.Compute(AnyArgs()).Returns(7);
        mockB.Compute(Any(), Any(), Any(), Any(), Any()).Returns(7);

        await Assert.That(mockA.Object.Compute(1, 2, 3, 4, 5)).IsEqualTo(7);
        await Assert.That(mockB.Object.Compute(1, 2, 3, 4, 5)).IsEqualTo(7);
    }

    [Test]
    public async Task AnyArgs_NotEmitted_For_Overloaded_Method_Names()
    {
        var mock = IOverloadedSumService.Mock();
        mock.Sum(Any(), Any()).Returns(11);
        mock.Sum(Any(), Any(), Any()).Returns(111);

        var svc = mock.Object;

        await Assert.That(svc.Sum(1, 2)).IsEqualTo(11);
        await Assert.That(svc.Sum(1, 2, 3)).IsEqualTo(111);

        await Assert.That(HasAnyArgsOverload("TUnit_Mocks_Tests_IOverloadedSumService_MockMemberExtensions", "Sum")).IsFalse();
        await Assert.That(HasAnyArgsOverload("TUnit_Mocks_Tests_IOverloadedSumService_MockMemberExtensions", "Notify")).IsFalse();
    }

    [Test]
    public async Task AnyArgs_Emitted_For_Unique_Method_Names()
    {
        await Assert.That(HasAnyArgsOverload("TUnit_Mocks_Tests_IFiveParamService_MockMemberExtensions", "Compute")).IsTrue();
        await Assert.That(HasAnyArgsOverload("TUnit_Mocks_Tests_IFiveParamService_MockMemberExtensions", "Log")).IsTrue();
    }

    // The generated extension class lives in TUnit.Mocks.Generated and is named
    // <safe-type-name>_MockMemberExtensions; the AnyArgs overload appears as a 2-param
    // extension method whose second parameter is typed AnyArgs.
    private static bool HasAnyArgsOverload(string extensionsTypeName, string methodName)
    {
        var extensionsType = typeof(AnyArgsTests).Assembly.GetTypes()
            .Single(t => t.Name == extensionsTypeName);
        return extensionsType.GetMethods()
            .Where(m => m.Name == methodName)
            .Any(m =>
            {
                var ps = m.GetParameters();
                return ps.Length == 2 && ps[1].ParameterType == typeof(AnyArgs);
            });
    }
}
