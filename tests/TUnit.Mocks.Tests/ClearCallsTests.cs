using TUnit.Mocks;
using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

public interface IClearNamed
{
    string Name { get; set; }
}

public interface IClearChildRepo
{
    void Save(int value);
}

public interface IClearParent
{
    IClearChildRepo Repo { get; }
}

public interface IClearNode
{
    IClearNode Next { get; }
    int Count(int value);
}

public class ClearCallsTests
{
    [Test]
    public async Task ClearCalls_Keeps_Sequenced_Setup_Position()
    {
        var mock = ICalculator.Mock();
        mock.Add(1, 2).ReturnsSequentially(10, 20, 30);
        ICalculator calc = mock.Object;

        await Assert.That(calc.Add(1, 2)).IsEqualTo(10);
        Mock.ClearCalls(mock);

        await Assert.That(calc.Add(1, 2)).IsEqualTo(20);
    }

    [Test]
    public async Task ClearCalls_Terminates_For_Self_Referencing_Auto_Mocks()
    {
        var node = IClearNode.Mock();
        var deep = node.Object.Next.Next.Next;
        deep.Count(1);

        Mock.ClearCalls(node);

        await Assert.That(Mock.Invocations(Mock.Get(deep)).Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClearCalls_Cascades_To_Cached_Auto_Mocks()
    {
        var parent = IClearParent.Mock();
        var child = parent.Object.Repo;
        child.Save(1);
        var childMock = Mock.Get(child);

        Mock.ClearCalls(parent);

        await Assert.That(Mock.Invocations(childMock).Count).IsEqualTo(0);
        childMock.Save(Any()).WasNeverCalled();
    }

    [Test]
    public async Task ClearCalls_Clears_History_But_Keeps_Setups()
    {
        var mock = ICalculator.Mock();
        mock.Add(1, 2).Returns(42);
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        calc.Add(1, 2);
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(2);

        Mock.ClearCalls(mock);

        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(0);
        await Assert.That(calc.Add(1, 2)).IsEqualTo(42);
    }

    [Test]
    public async Task ClearCalls_Resets_Verification_Counts()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        Mock.ClearCalls(mock);
        calc.Add(1, 2);

        mock.Add(1, 2).WasCalled(Times.Once);
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(1);
    }

    [Test]
    public async Task ClearCalls_Then_Never_Called_Passes()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        Mock.ClearCalls(mock);

        mock.Add(Any(), Any()).WasNeverCalled();
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClearCalls_Allows_VerifyNoOtherCalls_After_Fresh_Act()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;

        calc.Add(9, 9);
        Mock.ClearCalls(mock);
        calc.Add(1, 2);

        mock.Add(1, 2).WasCalled(Times.Once);
        Mock.VerifyNoOtherCalls(mock);
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(1);
    }

    [Test]
    public async Task ClearCalls_Resets_Setup_Invoke_Counts_For_VerifyAll()
    {
        var mock = ICalculator.Mock();
        mock.Add(1, 2).Returns(3);
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        Mock.VerifyAll(mock);

        Mock.ClearCalls(mock);

        await Assert.That(() => Mock.VerifyAll(mock)).Throws<MockVerificationException>();

        // The setup itself is kept and counts again once it is hit after the clear.
        await Assert.That(calc.Add(1, 2)).IsEqualTo(3);
        Mock.VerifyAll(mock);
    }

    [Test]
    public async Task ClearCalls_Keeps_Auto_Tracked_Properties()
    {
        var mock = IClearNamed.Mock();
        Mock.SetupAllProperties(mock);
        mock.Object.Name = "Alice";

        Mock.ClearCalls(mock);

        await Assert.That(mock.Object.Name).IsEqualTo("Alice");
    }

    [Test]
    public async Task ClearCalls_Static_Helper_Works()
    {
        var mock = ICalculator.Mock();
        mock.Object.Add(1, 2);

        Mock.ClearCalls(mock);

        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClearCalls_On_Fresh_Mock_Is_Noop()
    {
        var mock = ICalculator.Mock();

        Mock.ClearCalls(mock);

        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClearCalls_Does_Not_Break_Ordered_Verification()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;

        calc.Add(0, 0);
        Mock.ClearCalls(mock);
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
    public async Task MockRepository_ClearCalls_Clears_All_Tracked_Mocks()
    {
        var repo = new MockRepository();
        var calcMock = repo.Of<ICalculator>();
        var entityMock = repo.Of<IClearNamed>();
        calcMock.Add(1, 2).Returns(3);

        calcMock.Object.Add(1, 2);
        _ = entityMock.Object.Name;

        repo.ClearCalls();

        await Assert.That(Mock.Invocations(calcMock).Count).IsEqualTo(0);
        await Assert.That(Mock.Invocations(entityMock).Count).IsEqualTo(0);
        await Assert.That(calcMock.Object.Add(1, 2)).IsEqualTo(3);
    }

    [Test]
    public async Task ClearCalls_Is_Thread_Safe_With_Concurrent_Calls()
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
                Mock.ClearCalls(mock);
            }
        });

        await Task.WhenAll(callers.Append(clearer));
        Mock.ClearCalls(mock);

        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(0);
    }
}

#if NET9_0_OR_GREATER
public class MockControlPolyfillTests
{
    [Test]
    public async Task Instance_Style_ClearCalls_And_PassThrough_Are_Available()
    {
        var mock = PassThroughSubject.Mock();
        mock.PassThrough = false;
        mock.Object.Two(1, 2);

        await Assert.That(mock.Invocations.Count).IsEqualTo(1);
        mock.ClearCalls();

        await Assert.That(mock.Invocations.Count).IsEqualTo(0);
        await Assert.That(mock.PassThrough).IsFalse();
    }
}
#endif
