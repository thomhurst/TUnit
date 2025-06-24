using System.Threading.Tasks;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Attaches a custom property with a name-value pair to a test method, test class, or assembly.
/// These properties can be used for filtering tests or for providing additional metadata.
/// </summary>
/// <remarks>
/// Properties added with this attribute are available during test discovery and execution.
/// Multiple properties can be added to the same target by using multiple instances of this attribute.
/// </remarks>
/// <example>
/// <code>
/// [Property("Category", "Integration")]
/// [Property("Owner", "TestTeam")]
/// public class MyIntegrationTests
/// {
///     [Test]
///     [Property("Priority", "High")]
///     public void HighPriorityTest()
///     {
///         // Test implementation
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class PropertyAttribute(string name, string value) : TUnitAttribute, ITestDiscoveryEventReceiver
{
    /// <inheritdoc />
    public int Order => 0;

    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    public string Name { get; } = name;
    
    /// <summary>
    /// Gets the value of the property.
    /// </summary>
    public string Value { get; } = value;

    /// <inheritdoc />
    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.AddProperty(Name, Value);
        return default;
    }
}