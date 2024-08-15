using TUnit.Core;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator;

public static class WellKnownFullyQualifiedClassNames
{
    // Test Definition Attributes
    public static readonly FullyQualifiedTypeName BaseTestAttribute = typeof(BaseTestAttribute);

    public static readonly FullyQualifiedTypeName TestAttribute = typeof(TestAttribute);
    
    // Test Data Attributes
    public static readonly FullyQualifiedTypeName ArgumentsAttribute = typeof(ArgumentsAttribute);
    public static readonly FullyQualifiedTypeName MethodDataSourceAttribute = typeof(MethodDataSourceAttribute);
    public static readonly FullyQualifiedTypeName EnumerableMethodDataAttribute = typeof(EnumerableMethodDataSourceAttribute);
    public static readonly FullyQualifiedTypeName ClassDataSourceAttribute = typeof(ClassDataSourceAttribute<>);
    public static readonly FullyQualifiedTypeName MatrixAttribute = typeof(MatrixAttribute);
    
    // Metadata
    public static readonly FullyQualifiedTypeName TimeoutAttribute = typeof(TimeoutAttribute);
    public static readonly FullyQualifiedTypeName DisplayNameAttribute = typeof(DisplayNameAttribute);
 
    // Other
    public static readonly FullyQualifiedTypeName TestContext = typeof(TestContext);
    public static readonly FullyQualifiedTypeName CancellationToken = typeof(CancellationToken);
    public static readonly FullyQualifiedTypeName AssemblyHookContext = typeof(AssemblyHookContext);
    public static readonly FullyQualifiedTypeName ClassHookContext = typeof(ClassHookContext);
    public static readonly FullyQualifiedTypeName TestSessionContext = typeof(TestSessionContext);
    public static readonly FullyQualifiedTypeName TestDiscoveryContext = typeof(TestDiscoveryContext);
    public static readonly FullyQualifiedTypeName BeforeTestDiscoveryContext = typeof(BeforeTestDiscoveryContext);
    
    public static readonly FullyQualifiedTypeName BeforeAttribute = typeof(BeforeAttribute);
    public static readonly FullyQualifiedTypeName AfterAttribute = typeof(AfterAttribute);
        
    public static readonly FullyQualifiedTypeName BeforeEveryAttribute = typeof(BeforeEveryAttribute);
    public static readonly FullyQualifiedTypeName AfterEveryAttribute = typeof(AfterEveryAttribute);
}