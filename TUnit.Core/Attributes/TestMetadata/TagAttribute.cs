using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Adds a hierarchical tag to a test method, test class, or assembly for organizational and filtering purposes.
/// </summary>
/// <remarks>
/// <para>
/// Tags support dot-notation hierarchy (e.g., "integration.database.postgres").
/// When filtering, a parent tag matches all child tags:
/// filtering for "integration" matches "integration", "integration.database", and "integration.database.postgres".
/// </para>
///
/// <para>
/// The attribute can be applied at different levels:
/// </para>
/// <list type="bullet">
/// <item>Method level: Tags a specific test method</item>
/// <item>Class level: Tags all test methods in the class</item>
/// <item>Assembly level: Tags all test methods in the assembly</item>
/// </list>
///
/// <para>
/// Multiple tags can be applied to the same target by using multiple instances of this attribute.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Tag("integration.database")]
/// public class DatabaseTests
/// {
///     [Test]
///     [Tag("integration.database.postgres")]
///     public void PostgresTest()
///     {
///         // Test implementation
///     }
///
///     [Test]
///     [Tag("integration.database.sqlserver")]
///     [Tag("slow")]
///     public void SqlServerTest()
///     {
///         // Test implementation
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class TagAttribute(string tag) : TUnitAttribute, ITestDiscoveryEventReceiver
{
    /// <inheritdoc />
    public int Order => 0;

    /// <summary>
    /// Gets the tag value. Tags use dot notation for hierarchy (e.g., "integration.database.postgres").
    /// </summary>
    public string Tag { get; } = tag;

    /// <inheritdoc />
    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.AddTag(Tag);
        return default(ValueTask);
    }
}
