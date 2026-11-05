using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Helpers;

// Retains a tuple parameter's boundary and its mutable element boxes until invocation.
// A boxed ValueTuple would produce fresh struct copies whenever its elements are read.
internal sealed class TupleArgument(Type tupleType, object?[] elements) : IReadOnlyList<object?>
{
    public Type TupleType { get; } = tupleType;

    // Restore the exact runtime tuple type, including erased/private element types, only
    // after lifecycle consumers have finished mutating the retained element boxes.
    public object Materialize()
    {
        var index = 0;
        return Construct(TupleType, Elements, ref index);
    }

    private static readonly ConcurrentDictionary<Type, (ConstructorInfo Constructor, Type[] Arguments)> Constructors = new();

    // Root constructors on the generic definitions so every runtime tuple instantiation
    // remains invokable under trimming/Native AOT, even when generated code cannot name it.
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ValueTuple<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ValueTuple<,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ValueTuple<,,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ValueTuple<,,,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ValueTuple<,,,,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ValueTuple<,,,,,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ValueTuple<,,,,,,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ValueTuple<,,,,,,,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Tuple<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Tuple<,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Tuple<,,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Tuple<,,,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Tuple<,,,,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Tuple<,,,,,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Tuple<,,,,,,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Tuple<,,,,,,,>))]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Only System.Tuple and System.ValueTuple types reach this path; all their generic constructors are explicitly preserved above.")]
    private static object Construct(Type type, object?[] values, ref int index)
    {
        var shape = Constructors.GetOrAdd(type, static tuple =>
        {
            var types = tuple.GetGenericArguments();
            return (tuple.GetConstructor(types)!, types);
        });
        var arguments = new object?[shape.Arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
        {
            // Only the eighth generic argument is the flattened Rest slot. Tuple-valued
            // elements in earlier positions remain nested and consume one retained element.
            if (i == 7 && TupleHelper.IsTupleType(shape.Arguments[i]))
            {
                arguments[i] = Construct(shape.Arguments[i], values, ref index);
            }
            else
            {
                var value = values[index++];
                arguments[i] = value is TupleArgument nested ? nested.Materialize() : value;
            }
        }
        return shape.Constructor.Invoke(arguments);
    }

    public object?[] Elements { get; } = elements;
    public int Count => Elements.Length;
    public object? this[int index] => Elements[index];
    public IEnumerator<object?> GetEnumerator() => ((IEnumerable<object?>)Elements).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
