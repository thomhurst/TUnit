using TUnit.Mocks;
using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

public interface IClearNamed
{
    string Name { get; set; }
}

public class ClearInvocationsTests
{
    [Test]
    public async Task ClearInvocations_Clears_History_But_Keeps_Setups()
    {
        var mock = ICalculator.Mock();
        mock.Add(1, 2).Returns(42);
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        calc.Add(1, 2);
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(2);

        Mock.ClearInvocations(mock);

        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(0);
        await Assert.That(calc.Add(1, 2)).IsEqualTo(42);
    }

    [Test]
    public async Task ClearInvocations_Resets_Verification_Counts()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        Mock.ClearInvocations(mock);
        calc.Add(1, 2);

        mock.Add(1, 2).WasCalled(Times.Once);
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(1);
    }

    [Test]
    public async Task ClearInvocations_Then_Never_Called_Passes()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        Mock.ClearInvocations(mock);

        mock.Add(Any(), Any()).WasNeverCalled();
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClearInvocations_Allows_VerifyNoOtherCalls_After_Fresh_Act()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;

        calc.Add(9, 9);
        Mock.ClearInvocations(mock);
        calc.Add(1, 2);

        mock.Add(1, 2).WasCalled(Times.Once);
        Mock.VerifyNoOtherCalls(mock);
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(1);
    }

    [Test]
    public async Task ClearInvocations_Keeps_Setup_Invoked_State_For_VerifyAll()
    {
        var mock = ICalculator.Mock();
        mock.Add(1, 2).Returns(3);
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        Mock.ClearInvocations(mock);

        Mock.VerifyAll(mock);
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClearInvocations_Keeps_Auto_Tracked_Properties()
    {
        var mock = IClearNamed.Mock();
        Mock.SetupAllProperties(mock);
        mock.Object.Name = "Alice";

        Mock.ClearInvocations(mock);

        await Assert.That(mock.Object.Name).IsEqualTo("Alice");
    }

    [Test]
    public async Task ClearInvocations_Static_Helper_Works()
    {
        var mock = ICalculator.Mock();
        mock.Object.Add(1, 2);

        Mock.ClearInvocations(mock);

        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClearInvocations_On_Fresh_Mock_Is_Noop()
    {
        var mock = ICalculator.Mock();

        Mock.ClearInvocations(mock);

        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClearInvocations_Does_Not_Break_Ordered_Verification()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;

        calc.Add(0, 0);
        Mock.ClearInvocations(mock);
        calc.Add(1, 1);
        calc.Add(2, 2);

        Mock.VerifyInOrder(() =>
        {
            mock.Add(1, 1).WasCalled();
            mock.Add(2, 2).WasCalled();
        });
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(2);
    }

    [Test]
    public async Task MockRepository_ClearInvocations_Clears_All_Tracked_Mocks()
    {
        var repo = new MockRepository();
        var calcMock = repo.Of<ICalculator>();
        var entityMock = repo.Of<IClearNamed>();
        calcMock.Add(1, 2).Returns(3);

        calcMock.Object.Add(1, 2);
        _ = entityMock.Object.Name;

        repo.ClearInvocations();

        await Assert.That(Mock.Invocations(calcMock).Count).IsEqualTo(0);
        await Assert.That(Mock.Invocations(entityMock).Count).IsEqualTo(0);
        await Assert.That(calcMock.Object.Add(1, 2)).IsEqualTo(3);
    }

    [Test]
    public async Task ClearInvocations_Is_Thread_Safe_With_Concurrent_Calls()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;

        var callers = Enumerable.Range(0, 4)
            .Select(_ => Task.Run(() =>
            {
                for (var i = 0; i < 500; i++)
                {
                    calc.Add(i, i);
                }
            }))
            .ToArray();
        var clearer = Task.Run(() =>
        {
            for (var i = 0; i < 50; i++)
            {
                Mock.ClearInvocations(mock);
            }
        });

        await Task.WhenAll(callers.Append(clearer));
        Mock.ClearInvocations(mock);

        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(0);
    }
}

#if NET9_0_OR_GREATER
public class MockControlPolyfillTests
{
    [Test]
    public async Task Instance_Style_ClearInvocations_And_CallBase_Are_Available()
    {
        var mock = CallBaseSubject.Mock();
        mock.CallBase = false;
        mock.Object.Two(1, 2);

        await Assert.That(mock.Invocations.Count).IsEqualTo(1);
        mock.ClearInvocations();

        await Assert.That(mock.Invocations.Count).IsEqualTo(0);
        await Assert.That(mock.CallBase).IsFalse();
    }
}
#endif
