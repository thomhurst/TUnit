using System.Diagnostics;

namespace TUnit.Assertions.Tests;

[NotInParallel]
public class WaitsForAssertionTests
{
    [Test]
    public async Task WaitsFor_Passes_Immediately_When_Assertion_Succeeds()
    {
        var stopwatch = Stopwatch.StartNew();

        var value = 42;
        await Assert.That(value).WaitsFor(
            assert => assert.IsEqualTo(42),
            timeout: TimeSpan.FromSeconds(5));

        stopwatch.Stop();

        // Should complete very quickly since assertion passes immediately
        await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromMilliseconds(100));
    }

    [Test]
    public async Task WaitsFor_Passes_After_Multiple_Retries()
    {
        var counter = 0;
        var stopwatch = Stopwatch.StartNew();

        // Use a func that returns different values based on call count
        Func<int> getValue = () => Interlocked.Increment(ref counter);

        await Assert.That(getValue).WaitsFor(
            assert => assert.IsGreaterThan(3),
            timeout: TimeSpan.FromSeconds(5),
            pollingInterval: TimeSpan.FromMilliseconds(10));

        stopwatch.Stop();

        // Should have retried at least 3 times
        await Assert.That(counter).IsGreaterThanOrEqualTo(4);
    }

    [Test]
    public async Task WaitsFor_Fails_When_Timeout_Expires()
    {
        var stopwatch = Stopwatch.StartNew();
        var value = 1;

        var exception = await Assert.That(
            async () => await Assert.That(value).WaitsFor(
                assert => assert.IsEqualTo(999),
                timeout: TimeSpan.FromMilliseconds(100),
                pollingInterval: TimeSpan.FromMilliseconds(10))
        ).Throws<AssertionException>();

        stopwatch.Stop();

        // Verify timeout was respected (should be close to 100ms, not significantly longer)
        await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromMilliseconds(200));

        // Verify error message contains useful information
        await Assert.That(exception.Message).Contains("assertion did not pass within 100ms");
        await Assert.That(exception.Message).Contains("Last error:");
    }

    [Test]
    public async Task WaitsFor_Supports_And_Chaining()
    {
        var value = 42;

        // WaitsFor should support chaining with And
        await Assert.That(value)
            .WaitsFor(assert => assert.IsGreaterThan(40), timeout: TimeSpan.FromSeconds(1))
            .And.IsLessThan(50);
    }

    [Test]
    public async Task WaitsFor_Supports_Or_Chaining()
    {
        var value = 42;

        // WaitsFor should support chaining with Or
        await Assert.That(value)
            .WaitsFor(assert => assert.IsEqualTo(42), timeout: TimeSpan.FromSeconds(1))
            .Or.IsEqualTo(43);
    }

    [Test]
    public async Task WaitsFor_With_Custom_Polling_Interval()
    {
        var counter = 0;
        Func<int> getValue = () => Interlocked.Increment(ref counter);

        var stopwatch = Stopwatch.StartNew();

        await Assert.That(getValue).WaitsFor(
            assert => assert.IsGreaterThan(2),
            timeout: TimeSpan.FromSeconds(1),
            pollingInterval: TimeSpan.FromMilliseconds(50));

        stopwatch.Stop();

        // With 50ms interval and needing >2 (3rd increment), should take at least one polling interval
        // The timing may vary slightly due to execution overhead, so we check for at least 40ms
        await Assert.That(stopwatch.Elapsed).IsGreaterThan(TimeSpan.FromMilliseconds(40));

        // Verify counter was actually incremented multiple times
        await Assert.That(counter).IsGreaterThanOrEqualTo(3);
    }

    [Test]
    public async Task WaitsFor_With_Eventually_Changing_Value()
    {
        var value = 0;

        // Start a task that changes the value after 100ms
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            Interlocked.Exchange(ref value, 42);
        });

        // WaitsFor should poll and eventually see the new value
        await Assert.That(() => value).WaitsFor(
            assert => assert.IsEqualTo(42),
            timeout: TimeSpan.FromSeconds(5),
            pollingInterval: TimeSpan.FromMilliseconds(10));
    }

    [Test]
    public async Task WaitsFor_Works_With_Complex_Assertions()
    {
        var list = new List<int> { 1, 2, 3 };

        // Start a task that adds to the list after 50ms
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            lock (list)
            {
                list.Add(4);
                list.Add(5);
            }
        });

        // Wait for list to have 5 items
        await Assert.That(() =>
        {
            lock (list)
            {
                return list.Count;
            }
        }).WaitsFor(
            assert => assert.IsEqualTo(5),
            timeout: TimeSpan.FromSeconds(5),
            pollingInterval: TimeSpan.FromMilliseconds(10));
    }

    [Test]
    public async Task WaitsFor_Throws_ArgumentException_For_Zero_Timeout()
    {
        var value = 42;

#pragma warning disable TUnitAssertions0002 // Testing constructor exception, not awaiting
        var exception = Assert.Throws<ArgumentException>(() =>
            Assert.That(value).WaitsFor(
                assert => assert.IsEqualTo(42),
                timeout: TimeSpan.Zero));
#pragma warning restore TUnitAssertions0002

        await Assert.That(exception.Message).Contains("Timeout must be positive");
    }

    [Test]
    public async Task WaitsFor_Throws_ArgumentException_For_Negative_Timeout()
    {
        var value = 42;

#pragma warning disable TUnitAssertions0002 // Testing constructor exception, not awaiting
        var exception = Assert.Throws<ArgumentException>(() =>
            Assert.That(value).WaitsFor(
                assert => assert.IsEqualTo(42),
                timeout: TimeSpan.FromSeconds(-1)));
#pragma warning restore TUnitAssertions0002

        await Assert.That(exception.Message).Contains("Timeout must be positive");
    }

    [Test]
    public async Task WaitsFor_Throws_ArgumentException_For_Zero_PollingInterval()
    {
        var value = 42;

#pragma warning disable TUnitAssertions0002 // Testing constructor exception, not awaiting
        var exception = Assert.Throws<ArgumentException>(() =>
            Assert.That(value).WaitsFor(
                assert => assert.IsEqualTo(42),
                timeout: TimeSpan.FromSeconds(1),
                pollingInterval: TimeSpan.Zero));
#pragma warning restore TUnitAssertions0002

        await Assert.That(exception.Message).Contains("Polling interval must be positive");
    }

    [Test]
    public async Task WaitsFor_Throws_ArgumentNullException_For_Null_AssertionBuilder()
    {
        var value = 42;

#pragma warning disable TUnitAssertions0002 // Testing constructor exception, not awaiting
        var exception = Assert.Throws<ArgumentNullException>(() =>
            Assert.That(value).WaitsFor(
                assertionBuilder: null!,
                timeout: TimeSpan.FromSeconds(1)));
#pragma warning restore TUnitAssertions0002

        await Assert.That(exception.ParamName).IsEqualTo("assertionBuilder");
    }

    [Test]
    public async Task WaitsFor_Real_World_Scenario_GPIO_Event()
    {
        // Simulate the real-world scenario from the GitHub issue:
        // Testing GPIO events that take time to propagate

        var pinValue = false;

        // Simulate an async GPIO event that changes state after 75ms
        _ = Task.Run(async () =>
        {
            await Task.Delay(75);
            pinValue = true;
        });

        // Wait for the pin to become true
        await Assert.That(() => pinValue).WaitsFor(
            assert => assert.IsEqualTo(true),
            timeout: TimeSpan.FromSeconds(2),
            pollingInterval: TimeSpan.FromMilliseconds(10));
    }

    [Test]
    public async Task WaitsFor_Performance_Many_Quick_Polls()
    {
        var counter = 0;
        var stopwatch = Stopwatch.StartNew();

        // This will take many polls before succeeding
        Func<int> getValue = () => Interlocked.Increment(ref counter);

        await Assert.That(getValue).WaitsFor(
            assert => assert.IsGreaterThan(100),
            timeout: TimeSpan.FromSeconds(5),
            pollingInterval: TimeSpan.FromMilliseconds(1));

        stopwatch.Stop();

        // Should have made at least 100 attempts
        await Assert.That(counter).IsGreaterThanOrEqualTo(101);

        // Should complete in a reasonable time (well under 5 seconds)
        await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromSeconds(2));
    }
}
