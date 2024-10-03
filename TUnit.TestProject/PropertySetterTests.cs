namespace TUnit.TestProject;

public class PropertySetterTests
{
    [Arguments("1")]
    public required string Property1 { get; init; }
        
    [MethodDataSource(nameof(MethodData))]
    public required string Property2 { get; init; }
        
    [ClassDataSource<InnerModel>]
    public required InnerModel Property3 { get; init; }
        
    [DataSourceGeneratorTests.AutoFixtureGenerator<string>]
    public required string Property4 { get; init; }
    
    [Test]
    public void Test()
    {
    }

    public class InnerModel
    {
    }

    public static string MethodData() => "2";
}