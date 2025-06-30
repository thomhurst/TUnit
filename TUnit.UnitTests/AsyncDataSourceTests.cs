using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Assertions;

namespace TUnit.UnitTests;

public class AsyncDataSourceTests
{
    // Sync data source for comparison
    public static IEnumerable<object[]> SyncDataSource()
    {
        yield return new object[] { 1, "one" };
        yield return new object[] { 2, "two" };
        yield return new object[] { 3, "three" };
    }
    
    // Async data source returning Task<IEnumerable<T>>
    public static async Task<IEnumerable<object[]>> AsyncTaskDataSource()
    {
        await Task.Delay(10); // Simulate async work
        return new List<object[]>
        {
            new object[] { 10, "ten" },
            new object[] { 20, "twenty" },
            new object[] { 30, "thirty" }
        };
    }
    
    // Async data source returning ValueTask<IEnumerable<T>>
    public static async ValueTask<IEnumerable<object[]>> AsyncValueTaskDataSource()
    {
        await Task.Delay(5); // Simulate async work
        return new List<object[]>
        {
            new object[] { 100, "hundred" },
            new object[] { 200, "two hundred" }
        };
    }
    
    // Async enumerable data source
    public static async IAsyncEnumerable<object[]> AsyncEnumerableDataSource()
    {
        await Task.Delay(1);
        yield return new object[] { 1000, "thousand" };
        
        await Task.Delay(1);
        yield return new object[] { 2000, "two thousand" };
        
        await Task.Delay(1);
        yield return new object[] { 3000, "three thousand" };
    }
    
    // Async enumerable with cancellation support
    public static async IAsyncEnumerable<(int value, string text)> AsyncEnumerableWithCancellation(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < 5; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(10, cancellationToken);
            yield return (i * 1000, $"{i} thousand");
        }
    }
    
    [Test]
    [MethodDataSource(nameof(SyncDataSource))]
    public async Task TestWithSyncDataSource(int value, string text)
    {
        await Assert.That(value).IsGreaterThan(0);
        await Assert.That(text).IsNotNull();
        await Assert.That(text).IsNotEmpty();
    }
    
    [Test]
    [MethodDataSource(nameof(AsyncTaskDataSource))]
    public async Task TestWithAsyncTaskDataSource(int value, string text)
    {
        await Assert.That(value).IsGreaterThan(0);
        await Assert.That(text).IsNotNull();
        await Assert.That(text).Contains("t");
    }
    
    [Test]
    [MethodDataSource(nameof(AsyncValueTaskDataSource))]
    public async Task TestWithAsyncValueTaskDataSource(int value, string text)
    {
        await Assert.That(value).IsGreaterThanOrEqualTo(100);
        await Assert.That(text).IsNotNull();
        await Assert.That(text).Contains("hundred");
    }
    
    [Test]
    [MethodDataSource(nameof(AsyncEnumerableDataSource))]
    public async Task TestWithAsyncEnumerableDataSource(int value, string text)
    {
        await Assert.That(value).IsGreaterThanOrEqualTo(1000);
        await Assert.That(text).IsNotNull();
        await Assert.That(text).Contains("thousand");
    }
    
    [Test]
    [MethodDataSource(nameof(AsyncEnumerableWithCancellation))]
    public async Task TestWithAsyncEnumerableWithCancellation(int value, string text)
    {
        await Assert.That(value).IsGreaterThanOrEqualTo(0);
        await Assert.That(text).IsNotNull();
        await Assert.That(text).EndsWith("thousand");
    }
}

// Test with external class data source
public static class ExternalAsyncDataSources
{
    public static async IAsyncEnumerable<string[]> StringDataSource()
    {
        await Task.Yield();
        yield return new[] { "hello", "world" };
        yield return new[] { "foo", "bar" };
        yield return new[] { "async", "data" };
    }
}

public class ExternalAsyncDataSourceTests
{
    [Test]
    [MethodDataSource(typeof(ExternalAsyncDataSources), nameof(ExternalAsyncDataSources.StringDataSource))]
    public async Task TestWithExternalAsyncDataSource(string first, string second)
    {
        await Assert.That(first).IsNotNull();
        await Assert.That(second).IsNotNull();
        await Assert.That(first.Length).IsGreaterThan(0);
        await Assert.That(second.Length).IsGreaterThan(0);
    }
}