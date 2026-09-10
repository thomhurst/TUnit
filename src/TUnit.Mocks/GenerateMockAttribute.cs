namespace TUnit.Mocks;

/// <summary>
/// Instructs the TUnit.Mocks source generator to generate a mock for the specified type.
/// Use this for eager generation when no <c>T.Mock()</c> call is available to trigger discovery.
/// <para>
/// When the target type has static abstract members, the generator produces a bridge interface
/// (suffixed with <c>Mockable</c>) that provides Default Interface Method implementations.
/// Calling <c>T.Mock()</c> generates and uses this bridge automatically; <c>Mock.Of&lt;T&gt;()</c>
/// cannot be used because unresolved static abstract members trigger CS8920.
/// </para>
/// </summary>
/// <example>
/// <code>
/// [assembly: TUnit.Mocks.GenerateMock(typeof(IAmazonService))]
///
/// // In your test:
/// var mock = IAmazonService.Mock();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class GenerateMockAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="GenerateMockAttribute"/> with the type to mock.
    /// </summary>
    /// <param name="type">The type to generate a mock for. <c>typeof(T)</c> does not trigger CS8920.</param>
    public GenerateMockAttribute(Type type) { }
}
