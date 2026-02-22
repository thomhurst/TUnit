using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// T071 Integration Tests: Thread safety — concurrent mock usage, setup, and verification.
/// </summary>
public class ThreadSafetyTests
{
    [Test]
    public async Task Concurrent_Calls_Are_Thread_Safe()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);
        ICalculator calc = mock.Object;

        // Act — 100 concurrent calls
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() => calc.Add(1, 2)));
        var results = await Task.WhenAll(tasks);

        // Assert — all should return 42
        await Assert.That(results).HasCount().EqualTo(100);
        foreach (var result in results)
        {
            await Assert.That(result).IsEqualTo(42);
        }
    }

    [Test]
    public async Task Concurrent_Void_Calls_Are_Thread_Safe()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // Act — 100 concurrent void calls
        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() => calc.Log($"message-{i}")));
        await Task.WhenAll(tasks);

        // Assert — all 100 calls should be recorded
        mock.Verify.Log(Arg.Any<string>()).WasCalled(Times.Exactly(100));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Concurrent_Setup_And_Call()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // Act — one thread adds setups, others call methods simultaneously
        var setupTask = Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(i);
            }
        });

        var callTasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            // Call may or may not find a setup depending on timing — that is fine
            // The key is that no exception is thrown (no crash)
            return calc.Add(1, 2);
        }));

        var results = await Task.WhenAll(callTasks);
        await setupTask;

        // Assert — no exceptions were thrown, all results are valid ints
        await Assert.That(results).HasCount().EqualTo(50);
    }

    [Test]
    public async Task Concurrent_Calls_With_Different_Args()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(1, 1).Returns(10);
        mock.Setup.Add(2, 2).Returns(20);
        mock.Setup.Add(3, 3).Returns(30);
        ICalculator calc = mock.Object;

        // Act — concurrent calls with different args
        var tasks = Enumerable.Range(0, 90).Select(i =>
        {
            var group = (i % 3) + 1; // 1, 2, or 3
            return Task.Run(() => (group, result: calc.Add(group, group)));
        });
        var results = await Task.WhenAll(tasks);

        // Assert — each group returns the correct value
        foreach (var (group, result) in results)
        {
            var expected = group * 10;
            await Assert.That(result).IsEqualTo(expected);
        }
    }

    [Test]
    public async Task Concurrent_Verification_Is_Thread_Safe()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // Act — make some calls first
        for (int i = 0; i < 10; i++)
        {
            calc.Add(1, 2);
        }

        // Now verify concurrently from multiple threads
        var verifyTasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.Exactly(10));
        }));

        await Task.WhenAll(verifyTasks);

        // Assert — all verifications passed without exception
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Concurrent_Calls_On_Different_Interfaces()
    {
        // Arrange
        var calcMock = Mock.Of<ICalculator>();
        calcMock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(99);
        ICalculator calc = calcMock.Object;

        var greeterMock = Mock.Of<IGreeter>();
        greeterMock.Setup.Greet(Arg.Any<string>()).Returns("hi");
        IGreeter greeter = greeterMock.Object;

        // Act — concurrent calls on both mocks
        var calcTasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() => calc.Add(1, 2)));
        var greeterTasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() => greeter.Greet("test")));

        var calcResults = await Task.WhenAll(calcTasks);
        var greeterResults = await Task.WhenAll(greeterTasks);

        // Assert
        await Assert.That(calcResults).HasCount().EqualTo(50);
        await Assert.That(greeterResults).HasCount().EqualTo(50);

        foreach (var r in calcResults)
        {
            await Assert.That(r).IsEqualTo(99);
        }

        foreach (var r in greeterResults)
        {
            await Assert.That(r).IsEqualTo("hi");
        }
    }

    [Test]
    public async Task Concurrent_Calls_All_Recorded_In_History()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.Any<string>()).Returns("hello");
        IGreeter greeter = mock.Object;

        // Act — 100 concurrent calls
        var tasks = Enumerable.Range(0, 100).Select(i =>
            Task.Run(() => greeter.Greet($"user-{i}")));
        await Task.WhenAll(tasks);

        // Assert — verify total call count
        mock.Verify.Greet(Arg.Any<string>()).WasCalled(Times.Exactly(100));
        await Assert.That(true).IsTrue();
    }
}
