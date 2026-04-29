using System;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.Attributes;

/// <summary>
/// Marks a partial Should-flavored source wrapper class for which the source generator
/// should emit instance-method partials covering each public instance method on
/// <see cref="WrappedTypeDefinition"/>. The wrapper class is responsible for constructing
/// its own <see cref="AssertionContext{T}"/> rather than borrowing one from the wrapped
/// type's constructor.
/// </summary>
/// <remarks>
/// <para>
/// Used by <c>ShouldCollectionSource&lt;T&gt;</c> to expose <c>BeInOrder</c>/<c>All</c>/
/// <c>Any</c>/<c>HaveSingleItem</c>/etc. as instance methods so callers don't need
/// explicit type arguments — the generated extension form
/// <c>Method&lt;TCollection, TItem&gt;(IShouldSource&lt;TCollection&gt;)</c> can't infer
/// <c>TItem</c> from a constraint alone.
/// </para>
/// <para>
/// Pass the open generic form (<c>typeof(CollectionAssertion&lt;&gt;)</c>) when both wrapper
/// and wrapped are 1-arity generics: the source generator substitutes the wrapper's type
/// parameter into the wrapped definition. C# 11 generic attributes were the natural fit but
/// can't take a type parameter from the enclosing context as a type argument
/// (<c>CS8968</c>); the open-generic <c>Type</c> form is the workaround.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ShouldGeneratePartialAttribute : Attribute
{
    public ShouldGeneratePartialAttribute(Type wrappedTypeDefinition)
    {
        WrappedTypeDefinition = wrappedTypeDefinition;
    }

    /// <summary>
    /// The wrapped TUnit assertion type. Pass the open generic (<c>typeof(Foo&lt;&gt;)</c>)
    /// when the wrapper and wrapped have the same generic arity.
    /// </summary>
    public Type WrappedTypeDefinition { get; }
}
