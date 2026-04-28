using System;

namespace TUnit.Assertions.Should.Attributes;

/// <summary>
/// Marks a partial Should-flavored source wrapper class for which the source generator
/// should emit instance-method partials covering each public instance method on the
/// wrapped TUnit assertion type. The wrapped type is found by inspecting the class's
/// fields — the single field whose type derives from <c>Assertion&lt;T&gt;</c> is treated
/// as the inner instance to delegate to.
/// </summary>
/// <remarks>
/// Used by <c>ShouldCollectionSource&lt;T&gt;</c> to expose <c>BeInOrder</c>/<c>All</c>/
/// <c>Any</c>/<c>HaveSingleItem</c>/etc. as instance methods so callers don't need
/// explicit type arguments — the generated extension form
/// <c>Method&lt;TCollection, TItem&gt;(IShouldSource&lt;TCollection&gt;)</c> can't infer
/// <c>TItem</c> from a constraint alone.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ShouldGeneratePartialAttribute : Attribute;
