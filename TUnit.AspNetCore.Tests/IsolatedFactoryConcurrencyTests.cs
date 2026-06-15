using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.AspNetCore;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Regression tests for the race in <see cref="TestWebApplicationFactory{TEntryPoint}.GetIsolatedFactory"/>.
/// <para>
/// A shared (e.g. <c>SharedType.PerTestSession</c>) factory has <c>GetIsolatedFactory</c> invoked from
/// every test's parallel <c>Before(Test)</c> hook. Internally that calls
/// <see cref="WebApplicationFactory{TEntryPoint}.WithWebHostBuilder"/>, which appends to the base
/// factory's private <c>_derivedFactories</c> <see cref="List{T}"/> with no synchronization. Concurrent
/// <c>List.Add</c> calls tear the backing array (lost entries / null slots); the damage only surfaces
/// later as a <see cref="NullReferenceException"/> when the shared factory is disposed and enumerates
/// that list. These tests hammer the call concurrently and then dispose, reproducing that crash.
/// </para>
/// </summary>
public class IsolatedFactoryConcurrencyTests
{
    private const int Concurrency = 64;
    private const int Rounds = 8;

    [Test]
    public async Task Concurrent_GetIsolatedFactory_Then_Dispose_Does_Not_Throw()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var globalFactory = new TestWebAppFactory();

            var derived = await CreateIsolatedFactoriesConcurrentlyAsync(globalFactory);

            // Every call must have produced a distinct, non-null derived factory.
            await Assert.That(derived.Length).IsEqualTo(Concurrency);
            await Assert.That(derived.All(f => f is not null)).IsTrue();
            await Assert.That(derived.Distinct().Count()).IsEqualTo(Concurrency);

            // Disposing the shared factory enumerates _derivedFactories; a torn list NREs here.
            await Assert.That(async () => await globalFactory.DisposeAsync()).ThrowsNothing();
        }
    }

    private static async Task<WebApplicationFactory<Program>[]> CreateIsolatedFactoriesConcurrentlyAsync(
        TestWebAppFactory globalFactory)
    {
        var testContext = TestContext.Current!;
        var options = new WebApplicationTestOptions();

        // Release all workers at once to maximise the window for a concurrent List.Add tear.
        var gate = new TaskCompletionSource();

        var tasks = Enumerable.Range(0, Concurrency)
            .Select(_ => Task.Run(async () =>
            {
                await gate.Task;
                return globalFactory.GetIsolatedFactory(
                    testContext,
                    options,
                    static _ => { },
                    static (_, _) => { },
                    static _ => { });
            }))
            .ToArray();

        gate.SetResult();

        return await Task.WhenAll(tasks);
    }
}
