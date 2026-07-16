namespace TUnit.Mocks;

/// <summary>
/// Instructs the TUnit.Mocks source generator to generate a mock for the specified type.
/// This is required for interfaces with static abstract members (inherited or direct),
/// since <c>Mock.Of&lt;T&gt;()</c> triggers CS8920 when T has unresolved static abstract members.
/// <para>
/// When the target type has static abstract members, the generator produces a bridge interface
/// (suffixed with <c>_Mockable</c>) that provides Default Interface Method implementations.
/// Use <c>Mock.Of&lt;BridgeType&gt;()</c> to create the mock.
/// </para>
/// </summary>
/// <example>
/// <code>
/// [assembly: TUnit.Mocks.GenerateMock(typeof(IAmazonService))]
///
/// // In your test:
/// var mock = Mock.Of&lt;TUnit_Mocks_Tests_IAmazonService_Mockable&gt;();
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
