using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: [AssertionFrom] with unbound generic types (typeof(Box&lt;&gt;)), see #7132.
/// Should generate condition classes and extension methods that are generic over the type's parameters.
/// </summary>
public readonly struct Box<T>
{
    private readonly T[] _items;

    public Box(params T[] items) => _items = items;

    // bool property
    public bool IsEmpty => _items is null || _items.Length == 0;

    // bool instance method with a parameter of the type's own type parameter
    public bool Holds(T item) => _items is not null && System.Array.IndexOf(_items, item) >= 0;
}

public class Pair<TKey, TValue> where TKey : notnull
{
    public TKey? Key { get; set; }

    public bool HasKey() => Key is not null;
}

[AssertionFrom(typeof(Box<>), nameof(Box<int>.IsEmpty), CustomName = "IsEmptyBox")]
[AssertionFrom(typeof(Box<>), nameof(Box<int>.IsEmpty), CustomName = "IsNotEmptyBox", NegateLogic = true)]
[AssertionFrom(typeof(Box<>), nameof(Box<int>.Holds), CustomName = "HoldsItem")]
[AssertionFrom(typeof(Pair<,>), nameof(Pair<string, int>.HasKey))]
public static partial class UnboundGenericAssertions;
