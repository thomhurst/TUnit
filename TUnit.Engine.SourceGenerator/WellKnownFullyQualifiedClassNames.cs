using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator;

public static class WellKnownFullyQualifiedClassNames
{
    // Test Definition Attributes
    public static readonly FullyQualifiedTypeName BaseTestAttribute = "TUnit.Core.BaseTestAttribute";

    public static readonly FullyQualifiedTypeName TestAttribute = "TUnit.Core.TestAttribute";
    public static readonly FullyQualifiedTypeName DataDrivenTestAttribute = "TUnit.Core.DataDrivenTestAttribute";
    public static readonly FullyQualifiedTypeName DataSourceDrivenTestAttribute = "TUnit.Core.DataSourceDrivenTestAttribute";
    public static readonly FullyQualifiedTypeName CombinativeTestAttribute = "TUnit.Core.CombinativeTestAttribute";
    
    // Test Data Attributes
    public static readonly FullyQualifiedTypeName ArgumentsAttribute = "TUnit.Core.ArgumentsAttribute";
    public static readonly FullyQualifiedTypeName MethodDataSourceAttribute = "TUnit.Core.MethodDataSourceAttribute";
    public static readonly FullyQualifiedTypeName EnumerableMethodDataAttribute = "TUnit.Core.EnumerableMethodDataSourceAttribute";
    public static readonly FullyQualifiedTypeName ClassDataSourceAttribute = "TUnit.Core.ClassDataSourceAttribute";
    public static readonly FullyQualifiedTypeName CombinativeValuesAttribute = "TUnit.Core.CombinativeValuesAttribute";
    
    // Marker Attributes
    public static readonly FullyQualifiedTypeName InheritsTestsAttribute = "TUnit.Core.InheritsTestsAttribute";
    
    // Test Metadata Attributes
    public static readonly FullyQualifiedTypeName RepeatAttribute = "TUnit.Core.RepeatAttribute";
    public static readonly FullyQualifiedTypeName RetryAttribute = "TUnit.Core.RetryAttribute";
    public static readonly FullyQualifiedTypeName TimeoutAttribute = "TUnit.Core.TimeoutAttribute";
    public static readonly FullyQualifiedTypeName CustomPropertyAttribute = "TUnit.Core.PropertyAttribute";
    public static readonly FullyQualifiedTypeName DisplayNameAttribute = "TUnit.Core.DisplayNameAttribute";
    public static readonly FullyQualifiedTypeName NotInParallelAttribute = "TUnit.Core.NotInParallelAttribute";

    // Test Hooks Attributes
    public static readonly FullyQualifiedTypeName AssemblySetUpAttribute = "TUnit.Core.AssemblySetUpAttribute";
    public static readonly FullyQualifiedTypeName AssemblyCleanUpAttribute = "TUnit.Core.AssemblyCleanUpAttribute";
    public static readonly FullyQualifiedTypeName BeforeAllTestsInClassAttribute = "TUnit.Core.BeforeAllTestsInClassAttribute";
    public static readonly FullyQualifiedTypeName AfterAllTestsInClassAttribute = "TUnit.Core.AfterAllTestsInClassAttribute";
    public static readonly FullyQualifiedTypeName GlobalBeforeEachTestAttribute = "TUnit.Core.GlobalBeforeEachTestAttribute";
    public static readonly FullyQualifiedTypeName GlobalAfterEachTestAttribute = "TUnit.Core.GlobalAfterEachTestAttribute";

    // Interfaces
    public static readonly FullyQualifiedTypeName IBeforeTestAttribute = "TUnit.Core.Interfaces.IBeforeTestAttribute";
    public static readonly FullyQualifiedTypeName IAfterTestAttribute = "TUnit.Core.Interfaces.IAfterTestAttribute";
 
    // Other
    public static readonly FullyQualifiedTypeName TestContext = "TUnit.Core.TestContext";
    public static readonly FullyQualifiedTypeName CancellationToken = "System.Threading.CancellationToken";
}