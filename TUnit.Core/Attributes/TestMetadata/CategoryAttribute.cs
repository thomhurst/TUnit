using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Adds a category to a test method, test class, or assembly for organizational and filtering purposes.
/// </summary>
/// <remarks>
/// <para>
/// Categories provide a way to group tests for better organization and selective execution.
/// Tests can be filtered by category when running tests through the TUnit test runner.
/// </para>
/// 
/// <para>
/// The attribute can be applied at different levels:
/// </para>
/// <list type="bullet">
/// <item>Method level: Categorizes a specific test method</item>
/// <item>Class level: Categorizes all test methods in the class</item>
/// <item>Assembly level: Categorizes all test methods in the assembly</item>
/// </list>
/// 
/// <para>
/// Multiple categories can be applied to the same target by using multiple instances of this attribute.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Category("Integration")]
/// public class IntegrationTests
/// {
///     [Test]
///     [Category("Database")]
///     public void DatabaseTest()
///     {
///         // Test implementation
///     }
///     
///     [Test]
///     [Category("API")]
///     [Category("Authentication")]
///     public void ApiAuthenticationTest()
///     {
///         // Test implementation
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class CategoryAttribute(string category) : TUnitAttribute, ITestDiscoveryEventReceiver
{
    /// <inheritdoc />
    public int Order => 0;

    /// <summary>
    /// Gets the name of the category.
    /// </summary>
    public string Category { get; } = category;
    
    /// <inheritdoc />
    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.AddCategory(Category);
    }
}