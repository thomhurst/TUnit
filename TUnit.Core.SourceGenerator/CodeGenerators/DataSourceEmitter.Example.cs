// Example of how the source generator should emit data sources with inline delegates

namespace TUnit.Core.SourceGenerator.Examples;

/// <summary>
/// Example showing how data sources should be emitted by the source generator
/// </summary>
public static class DataSourceEmitterExample
{
    // Instead of this (old approach with registry):
    /*
    DataSources = new TestDataSource[]
    {
        new DynamicTestDataSource(true) 
        { 
            FactoryKey = "global::MyTestClass.GetTestData" 
        }
    }
    */
    
    // Emit this (new approach with inline delegates):
    public static string EmitMethodDataSource(string className, string methodName, bool isShared)
    {
        return $@"new DelegateDataSource(() => {className}.{methodName}(), {isShared.ToString().ToLower()})";
    }
    
    // For async data sources:
    public static string EmitAsyncDataSource(string className, string methodName, bool isShared)
    {
        return $@"new AsyncDelegateDataSource((ct) => {className}.{methodName}(ct), {isShared.ToString().ToLower()})";
    }
    
    // For Task<IEnumerable<T>> data sources:
    public static string EmitTaskDataSource(string className, string methodName, bool isShared)
    {
        return $@"new TaskDelegateDataSource(() => {className}.{methodName}(), {isShared.ToString().ToLower()})";
    }
    
    // Example of complete TestMetadata emission:
    public static string EmitTestMetadata()
    {
        return @"
new TestMetadata
{
    TestId = ""Test123"",
    TestName = ""MyTest"",
    TestClassType = typeof(MyTestClass),
    TestMethodName = ""TestMethod"",
    
    // Method data sources (for test parameters)
    DataSources = new TestDataSource[]
    {
        new DelegateDataSource(() => MyTestClass.GetTestData(), false),
        new AsyncDelegateDataSource((ct) => MyTestClass.GetAsyncTestData(ct), true),
        new StaticTestDataSource(new object[][] { new object[] { 1, ""test"" }, new object[] { 2, ""test2"" } })
    },
    
    // Class data sources (for constructor parameters)
    ClassDataSources = new TestDataSource[]
    {
        new DelegateDataSource(() => MyTestClass.GetConstructorData(), true)
    },
    
    // Property data sources (for property injection)
    PropertyDataSources = new PropertyDataSource[]
    {
        new PropertyDataSource
        {
            PropertyName = ""TestProperty"",
            PropertyType = typeof(string),
            DataSource = new DelegateDataSource(() => MyTestClass.GetPropertyData(), false)
        }
    }
}";
    }
    
    // For complex data source with type conversion:
    public static string EmitComplexDataSource(string dataSourceExpression, string elementType)
    {
        return $@"new DelegateDataSource(() => 
    ConvertToObjectArrays({dataSourceExpression}, typeof({elementType})), false)";
    }
    
    // Helper method that would be included in generated code:
    public const string ConvertToObjectArraysHelper = @"
private static IEnumerable<object?[]> ConvertToObjectArrays<T>(IEnumerable<T> source, Type elementType)
{
    foreach (var item in source)
    {
        if (item is object?[] array)
        {
            yield return array;
        }
        else if (elementType.IsAssignableTo(typeof(ITuple)))
        {
            // Handle tuples
            var tuple = item as ITuple;
            if (tuple != null)
            {
                var values = new object?[tuple.Length];
                for (int i = 0; i < tuple.Length; i++)
                {
                    values[i] = tuple[i];
                }
                yield return values;
            }
        }
        else
        {
            // Single value
            yield return new object?[] { item };
        }
    }
}";
}