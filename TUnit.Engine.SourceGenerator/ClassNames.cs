namespace TUnit.Engine.SourceGenerator;

public static class ClassNames
{
    // Test Definition Attributes
    public static readonly string BaseTestAttribute = "BaseTestAttribute";

    public static readonly string TestAttribute = "TestAttribute";
    public static readonly string DataDrivenTestAttribute = "DataDrivenTestAttribute";
    public static readonly string DataSourceDrivenTestAttribute = "DataSourceDrivenTestAttribute";
    public static readonly string CombinativeTestAttribute = "CombinativeTestAttribute";
    
    // Test Data Attributes
    public static readonly string ArgumentsAttribute = "ArgumentsAttribute";
    public static readonly string MethodDataSourceAttribute = "MethodDataSourceAttribute";
    public static readonly string EnumerableMethodDataAttribute = "EnumerableMethodDataSourceAttribute";
    public static readonly string ClassDataSourceAttribute = "ClassDataSourceAttribute";
    public static readonly string CombinativeValuesAttribute = "CombinativeValuesAttribute";
    
    // Marker Attributes
    public static readonly string InheritsTestsAttribute = "InheritsTestsAttribute";
    
    // Test Metadata Attributes
    public static readonly string RepeatAttribute = "RepeatAttribute";
    public static readonly string RetryAttribute = "RetryAttribute";
    public static readonly string TimeoutAttribute = "TimeoutAttribute";
    public static readonly string CustomPropertyAttribute = "PropertyAttribute";
    public static readonly string DisplayNameAttribute = "DisplayNameAttribute";
    public static readonly string NotInParallelAttribute = "NotInParallelAttribute";

    // Test Hooks Attributes
    public static readonly string AssemblySetUpAttribute = "AssemblySetUpAttribute";
    public static readonly string AssemblyCleanUpAttribute = "AssemblyCleanUpAttribute";
    public static readonly string BeforeAllTestsInClassAttribute = "BeforeAllTestsInClassAttribute";
    public static readonly string AfterAllTestsInClassAttribute = "AfterAllTestsInClassAttribute";
    public static readonly string GlobalBeforeEachTestAttribute = "GlobalBeforeEachTestAttribute";
    public static readonly string GlobalAfterEachTestAttribute = "GlobalAfterEachTestAttribute";

    // Interfaces
    public static readonly string IBeforeTestAttribute = "IBeforeTestAttribute";
    public static readonly string IAfterTestAttribute = "IAfterTestAttribute";
 
    // Other
    public static readonly string TestContext = "TestContext";
    public static readonly string CancellationToken = "global::System.Threading.CancellationToken";
}