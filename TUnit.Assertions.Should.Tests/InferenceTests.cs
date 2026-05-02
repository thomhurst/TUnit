using TUnit.Assertions.Should;
using TUnit.Assertions.Should.Extensions;

namespace TUnit.Assertions.Should.Tests;

/// <summary>
/// Verifies that element-type inference works without any explicit generic type arguments.
/// If any of these tests require a manual cast or explicit <c>.Should&lt;T&gt;()</c>, the generator
/// is missing covariance handling for that source type.
/// </summary>
public class InferenceTests
{
    [Test]
    public async Task List_int_Contain_no_explicit_generic()
    {
        var list = new List<int> { 1, 2, 3 };
        await list.Should().Contain(2);
    }

    [Test]
    public async Task ReadOnlyCollection_string_Contain_no_explicit_generic()
    {
        IReadOnlyCollection<string> coll = new[] { "a", "b", "c" };
        await coll.Should().Contain("b");
    }

    [Test]
    public async Task HashSet_Contain_no_explicit_generic()
    {
        var set = new HashSet<int> { 1, 2, 3 };
        await set.Should().Contain(2);
    }

    [Test]
    public async Task Stack_Contain_no_explicit_generic()
    {
        var stack = new Stack<int>();
        stack.Push(1);
        stack.Push(2);
        await stack.Should().Contain(1);
    }

    [Test]
    public async Task Queue_Contain_no_explicit_generic()
    {
        var queue = new Queue<string>();
        queue.Enqueue("a");
        queue.Enqueue("b");
        await queue.Should().Contain("a");
    }

    [Test]
    public async Task Custom_collection_Contain_no_explicit_generic()
    {
        IEnumerable<DateTime> dates = new[] { DateTime.MinValue, DateTime.MaxValue };
        await dates.Should().Contain(DateTime.MinValue);
    }
}
