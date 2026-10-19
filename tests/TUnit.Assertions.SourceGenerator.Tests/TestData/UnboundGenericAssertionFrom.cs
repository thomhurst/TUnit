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

public class Shadow<T>
{
    public T? Value { get; set; }

    // A method type parameter that cannot be inferred from the arguments, so the generated call must pass it
    public bool HoldsValueOfType<TOther>() => Value is TOther;

#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type
    // A method type parameter that reuses the type's parameter name
    public bool IsDefaultOf<T>(T value) => Equals(value, default(T));
#pragma warning restore CS0693
}

public class Outer<T>
{
#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type
    // A nested type parameter that reuses the containing type's parameter name
    public class Inner<T>
    {
        public bool IsSet { get; set; }
    }
#pragma warning restore CS0693
}

[AssertionFrom(typeof(Box<>), nameof(Box<int>.IsEmpty), CustomName = "IsEmptyBox")]
[AssertionFrom(typeof(Box<>), nameof(Box<int>.IsEmpty), CustomName = "IsNotEmptyBox", NegateLogic = true)]
[AssertionFrom(typeof(Box<>), nameof(Box<int>.Holds), CustomName = "HoldsItem")]
[AssertionFrom(typeof(Pair<,>), nameof(Pair<string, int>.HasKey))]
[AssertionFrom(typeof(Shadow<>), nameof(Shadow<int>.HoldsValueOfType))]
[AssertionFrom(typeof(Shadow<>), nameof(Shadow<int>.IsDefaultOf))]
[AssertionFrom(typeof(Outer<>.Inner<>), nameof(Outer<int>.Inner<int>.IsSet))]
// The same member on a closed type needs its own, non-generic, assertion class
[AssertionFrom(typeof(Box<int>), nameof(Box<int>.IsEmpty), CustomName = "IsEmptyIntBox")]
public static partial class UnboundGenericAssertions;
