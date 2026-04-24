#pragma warning disable TUnitAssertions0002 // Assert statements must be awaited — inspecting returned assertion type
using System.Collections;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Tests.Bugs.Issue5692;

/// <summary>
/// Regression guard for https://github.com/thomhurst/TUnit/issues/5692.
///
/// The fix replaces the <c>Assert.That(string?)</c> parameter with a wrapper struct so that user
/// types declaring <c>implicit operator string</c> no longer silently bind to the string overload
/// and trigger a <see cref="NullReferenceException"/> at the call site.
///
/// These tests verify the bug is fixed AND lock down every other <c>Assert.That</c> routing path
/// so the change cannot silently re-route values between assertion types.
/// </summary>
public class ImplicitStringOperatorOverloadTests
{
    public record Id(string Value)
    {
        public static implicit operator string(Id id) => id.Value;
    }

    public sealed class Envelope
    {
        public Id? ConversationId { get; set; }
    }

    // ============ Bug #5692: implicit-to-string must not hit string overload ============

    [Test]
    public async Task NullId_WithImplicitToString_IsNull_DoesNotNRE()
    {
        var envelope = new Envelope();
        await Assert.That(envelope.ConversationId).IsNull();
    }

    [Test]
    public async Task NullId_WithImplicitToString_IsEqualTo_DoesNotNRE()
    {
        var envelope = new Envelope();
        await Assert.That(envelope.ConversationId).IsEqualTo((Id?)null);
    }

    [Test]
    public async Task NonNullId_WithImplicitToString_RoutesToValueAssertionOfId()
    {
        var id = new Id("abc");
        var assertion = Assert.That(id);
        await Assert.That(assertion).IsTypeOf<ValueAssertion<Id>>();
    }

    // ============ String: static overload still wins (no char-collection routing) ============

    [Test]
    public async Task StringLiteral_RoutesToValueAssertionOfString()
    {
        var assertion = Assert.That("hello");
        await Assert.That(assertion).IsTypeOf<ValueAssertion<string>>();
    }

    [Test]
    public async Task StringVariable_RoutesToValueAssertionOfString()
    {
        string value = "hello";
        var assertion = Assert.That(value);
        await Assert.That(assertion).IsTypeOf<ValueAssertion<string>>();
    }

    [Test]
    public async Task NullableStringVariable_Null_RoutesToValueAssertionOfString()
    {
        string? value = null;
        var assertion = Assert.That(value);
        await Assert.That(assertion).IsTypeOf<ValueAssertion<string>>();
    }

    [Test]
    public async Task NullableStringVariable_NonNull_RoutesToValueAssertionOfString()
    {
        string? value = "hello";
        var assertion = Assert.That(value);
        await Assert.That(assertion).IsTypeOf<ValueAssertion<string>>();
    }

    [Test]
    public async Task String_IsEqualTo_StillCompares_AsString()
    {
        await Assert.That("hello").IsEqualTo("hello");
    }

    // ============ Explicit char-collection casts still route to CollectionAssertion<char> ============

    [Test]
    public async Task StringCastToIEnumerableOfChar_RoutesToCollectionAssertion()
    {
        var assertion = Assert.That((IEnumerable<char>)"ABC");
        await Assert.That(assertion).IsTypeOf<CollectionAssertion<char>>();
    }

    [Test]
    public async Task StringCastToIEnumerableOfChar_Contains_Works()
    {
        await Assert.That((IEnumerable<char>)"ABC").Contains('B');
    }

    [Test]
    public async Task StringToCharArray_RoutesToArrayAssertion()
    {
        var assertion = Assert.That("ABC".ToCharArray());
        await Assert.That(assertion).IsTypeOf<ArrayAssertion<char>>();
    }

    // ============ Collection overloads unchanged ============

    [Test]
    public async Task IntArray_RoutesToArrayAssertion()
    {
        int[] arr = [1, 2, 3];
        var assertion = Assert.That(arr);
        await Assert.That(assertion).IsTypeOf<ArrayAssertion<int>>();
    }

    [Test]
    public async Task ListOfInt_RoutesToListAssertion()
    {
        List<int> list = [1, 2, 3];
        var assertion = Assert.That(list);
        await Assert.That(assertion).IsTypeOf<ListAssertion<int>>();
    }

    [Test]
    public async Task IListOfInt_RoutesToListAssertion()
    {
        IList<int> list = new List<int> { 1, 2, 3 };
        var assertion = Assert.That(list);
        await Assert.That(assertion).IsTypeOf<ListAssertion<int>>();
    }

    [Test]
    public async Task IReadOnlyListOfInt_RoutesToReadOnlyListAssertion()
    {
        IReadOnlyList<int> list = new List<int> { 1, 2, 3 };
        var assertion = Assert.That(list);
        await Assert.That(assertion).IsTypeOf<ReadOnlyListAssertion<int>>();
    }

    [Test]
    public async Task HashSetOfInt_RoutesToHashSetAssertion()
    {
        var set = new HashSet<int> { 1, 2, 3 };
        var assertion = Assert.That(set);
        await Assert.That(assertion).IsTypeOf<HashSetAssertion<int>>();
    }

    [Test]
    public async Task IEnumerableOfInt_RoutesToCollectionAssertion()
    {
        IEnumerable<int> items = new[] { 1, 2, 3 };
        var assertion = Assert.That(items);
        await Assert.That(assertion).IsTypeOf<CollectionAssertion<int>>();
    }

    [Test]
    public async Task IQueryableOfInt_RoutesToCollectionAssertion()
    {
        IQueryable<int> queryable = new[] { 1, 2, 3 }.AsQueryable();
        var assertion = Assert.That(queryable);
        await Assert.That(assertion).IsTypeOf<CollectionAssertion<int>>();
    }

    [Test]
    public async Task NonGenericIEnumerable_RoutesToCollectionAssertionOfObject()
    {
        IEnumerable enumerable = new ArrayList { 1, 2, 3 };
        var assertion = Assert.That(enumerable);
        await Assert.That(assertion).IsTypeOf<CollectionAssertion<object?>>();
    }

    [Test]
    public async Task DictionaryOfStringInt_RoutesToMutableDictionaryAssertion()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1 };
        var assertion = Assert.That(dict);
        await Assert.That(assertion).IsTypeOf<MutableDictionaryAssertion<string, int>>();
    }

    [Test]
    public async Task MemoryOfChar_RoutesToMemoryAssertion()
    {
        Memory<char> mem = new[] { 'a', 'b', 'c' };
        var assertion = Assert.That(mem);
        await Assert.That(assertion).IsTypeOf<MemoryAssertion<char>>();
    }

    [Test]
    public async Task ReadOnlyMemoryOfChar_RoutesToReadOnlyMemoryAssertion()
    {
        ReadOnlyMemory<char> mem = new[] { 'a', 'b', 'c' };
        var assertion = Assert.That(mem);
        await Assert.That(assertion).IsTypeOf<ReadOnlyMemoryAssertion<char>>();
    }

    // ============ Delegate / Task overloads unchanged ============

    [Test]
    public async Task Action_RoutesToDelegateAssertion()
    {
        Action action = () => { };
        var assertion = Assert.That(action);
        await Assert.That(assertion).IsTypeOf<DelegateAssertion>();
    }

    [Test]
    public async Task AsyncLambda_RoutesToAsyncDelegateAssertion()
    {
        Func<Task> action = () => Task.CompletedTask;
        var assertion = Assert.That(action);
        await Assert.That(assertion).IsTypeOf<AsyncDelegateAssertion>();
    }

    [Test]
    public async Task Task_RoutesToAsyncDelegateAssertion()
    {
        Task task = Task.CompletedTask;
        var assertion = Assert.That(task);
        await Assert.That(assertion).IsTypeOf<AsyncDelegateAssertion>();
    }

    [Test]
    public async Task TaskOfInt_RoutesToTaskAssertion()
    {
        Task<int> task = Task.FromResult(42);
        var assertion = Assert.That(task);
        await Assert.That(assertion).IsTypeOf<TaskAssertion<int>>();
    }

    [Test]
    public async Task FuncOfInt_RoutesToFuncAssertion()
    {
        Func<int> func = () => 42;
        var assertion = Assert.That(func);
        await Assert.That(assertion).IsTypeOf<FuncAssertion<int>>();
    }

    [Test]
    public async Task FuncOfEnumerable_RoutesToFuncCollectionAssertion()
    {
        Func<IEnumerable<int>> func = () => new[] { 1, 2, 3 };
        var assertion = Assert.That(func);
        await Assert.That(assertion).IsTypeOf<FuncCollectionAssertion<int>>();
    }

    // ============ Value-type arguments unchanged ============

    [Test]
    public async Task Int_RoutesToValueAssertionOfInt()
    {
        var assertion = Assert.That(42);
        await Assert.That(assertion).IsTypeOf<ValueAssertion<int>>();
    }

    [Test]
    public async Task NullableInt_RoutesToValueAssertion()
    {
        int? value = 42;
        var assertion = Assert.That(value);
        await Assert.That(assertion).IsTypeOf<ValueAssertion<int?>>();
    }

    [Test]
    public async Task CustomRefType_WithoutImplicitToString_RoutesToValueAssertion()
    {
        var obj = new Uri("http://example.com");
        var assertion = Assert.That(obj);
        await Assert.That(assertion).IsTypeOf<ValueAssertion<Uri>>();
    }
}
