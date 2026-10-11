namespace TUnit.Mocks.SourceGenerator.Models;

/// <summary>
/// A closed generic interface named at a <c>Mock.Of&lt;T&gt;()</c> call site or in
/// <c>[assembly: GenerateMock(typeof(T))]</c>, e.g. <c>IRepository&lt;int&gt;</c>.
/// </summary>
/// <remarks>
/// Generic interface mocks are generated once, as open types. Without a closed registration the
/// runtime builds <c>IRepository&lt;int&gt;</c> through <c>MakeGenericType</c>, which Native AOT
/// cannot do for value-type arguments. Registering the closed factory statically roots the
/// instantiation and skips reflection entirely. See issue #7027.
/// </remarks>
/// <param name="OpenTypeFullyQualifiedName">Matches the open model's <c>FullyQualifiedName</c>.</param>
/// <param name="ClosedTypeFullyQualifiedName">The closed interface, e.g. <c>global::Ns.IRepository&lt;int&gt;</c>.</param>
/// <param name="TypeArguments">Every type argument, containing types first, in model type-parameter order.</param>
internal sealed record ClosedGenericMockRegistration(
    string OpenTypeFullyQualifiedName,
    string ClosedTypeFullyQualifiedName,
    EquatableArray<string> TypeArguments);

/// <summary>
/// A <see cref="ClosedGenericMockRegistration"/> resolved against the open mock that was actually
/// generated for it, ready to emit.
/// </summary>
/// <param name="ClosedTypeFullyQualifiedName">The closed interface to register.</param>
/// <param name="FactoryMethod">The closed generated <c>CreateAutoMock&lt;...&gt;</c> method to call.</param>
internal readonly record struct ClosedGenericFactoryEmission(
    string ClosedTypeFullyQualifiedName,
    string FactoryMethod);
