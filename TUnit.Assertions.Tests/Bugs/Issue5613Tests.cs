namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for GitHub issue #5613 findings #1 and #3:
/// Assert.That(Task task) returns AsyncDelegateAssertion which implements both
/// IAssertionSource&lt;object?&gt; and IAssertionSource&lt;Task&gt;. This ambiguity breaks
/// generic inference on IsNotNull / IsNull / IsSameReferenceAs / IsNotSameReferenceAs
/// (CS0411) and produces a misleading CS1929 pointing at JsonElement.
///
/// Fix: instance methods on AsyncDelegateAssertion that forward to the
/// IAssertionSource&lt;Task&gt; interface.
/// </summary>
public class Issue5613Tests
{
    [Test]
    public async Task IsNotNull_On_Task_Reference_Compiles_And_Passes()
    {
        Task t = Task.CompletedTask;
        await Assert.That(t).IsNotNull();
    }

    [Test]
    public async Task IsNull_On_Null_Task_Reference_Compiles_And_Passes()
    {
        Task? t = null;
        await Assert.That(t!).IsNull();
    }

    [Test]
    public async Task IsSameReferenceAs_On_Task_Compiles_Without_Generic_Annotation()
    {
        Task a = Task.CompletedTask;
        Task b = a;
        await Assert.That(a).IsSameReferenceAs(b);
    }

    [Test]
    public async Task IsNotSameReferenceAs_On_Task_Compiles_Without_Generic_Annotation()
    {
        Task a = Task.CompletedTask;
        Task b = Task.FromResult(true);
        await Assert.That(a).IsNotSameReferenceAs(b);
    }
}
