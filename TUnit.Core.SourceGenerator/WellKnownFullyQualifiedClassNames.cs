using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator;

public static class WellKnownFullyQualifiedClassNames
{
    // Test Definition Attributes
    public static readonly FullyQualifiedTypeName BaseTestAttribute = "TUnit.Core.BaseTestAttribute";

    public static readonly FullyQualifiedTypeName TestAttribute = "TUnit.Core.TestAttribute";
    
    // Test Data Attributes
    public static readonly FullyQualifiedTypeName ArgumentsAttribute = "TUnit.Core.ArgumentsAttribute";
    public static readonly FullyQualifiedTypeName MethodDataSourceAttribute = "TUnit.Core.MethodDataSourceAttribute";
    public static readonly FullyQualifiedTypeName ClassDataSourceAttribute = "TUnit.Core.ClassDataSourceAttribute";
    public static readonly FullyQualifiedTypeName MatrixAttribute = "TUnit.Core.MatrixAttribute";
    public static readonly FullyQualifiedTypeName ClassConstructorAttribute = "TUnit.Core.ClassConstructorAttribute";
    public static readonly FullyQualifiedTypeName DataSourceGeneratorAttribute = "TUnit.Core.DataSourceGeneratorAttribute";

    
    // Metadata
    public static readonly FullyQualifiedTypeName TimeoutAttribute = "TUnit.Core.TimeoutAttribute";
    public static readonly FullyQualifiedTypeName DisplayNameAttribute = "TUnit.Core.DisplayNameAttribute";
 
    // Other
    public static readonly FullyQualifiedTypeName TestContext = "TUnit.Core.TestContext";
    public static readonly FullyQualifiedTypeName AssemblyHookContext = "TUnit.Core.AssemblyHookContext";
    public static readonly FullyQualifiedTypeName ClassHookContext = "TUnit.Core.ClassHookContext";
    public static readonly FullyQualifiedTypeName TestSessionContext = "TUnit.Core.TestSessionContext";
    public static readonly FullyQualifiedTypeName TestDiscoveryContext = "TUnit.Core.TestDiscoveryContext";
    public static readonly FullyQualifiedTypeName BeforeTestDiscoveryContext = "TUnit.Core.BeforeTestDiscoveryContext";
    
    public static readonly FullyQualifiedTypeName BeforeAttribute = "TUnit.Core.BeforeAttribute";
    public static readonly FullyQualifiedTypeName AfterAttribute = "TUnit.Core.AfterAttribute";
        
    public static readonly FullyQualifiedTypeName BeforeEveryAttribute = "TUnit.Core.BeforeEveryAttribute";
    public static readonly FullyQualifiedTypeName AfterEveryAttribute = "TUnit.Core.AfterEveryAttribute";
    
    public static readonly FullyQualifiedTypeName CancellationToken = "System.Threading.CancellationToken";
    
    public static readonly FullyQualifiedTypeName IDataAttribute = "TUnit.Core.IDataAttribute";
    public static readonly FullyQualifiedTypeName IDataSourceGeneratorAttribute = "TUnit.Core.IDataSourceGeneratorAttribute";
}