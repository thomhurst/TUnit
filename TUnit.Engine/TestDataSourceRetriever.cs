using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine;

internal class TestDataSourceRetriever(MethodInvoker methodInvoker)
{
    public IEnumerable<object?[]?> GetTestDataSourceArguments(MethodInfo methodInfo)
    {
        var testDataSourceAttributes = methodInfo.GetCustomAttributes<TestDataSourceAttribute>().ToList();

        if (!testDataSourceAttributes.Any())
        {
            yield return null;
            yield break;
        }
        
        foreach (var testDataSourceAttribute in testDataSourceAttributes)
        {
            yield return GetTestDataSourceArguments(methodInfo.DeclaringType!, testDataSourceAttribute);
        }
    }
    
    public IEnumerable<object?[]?> GetTestDataSourceArguments(Type type)
    {
        var testDataSourceAttributes = type.GetCustomAttributes<TestDataSourceAttribute>().ToList();

        if (!testDataSourceAttributes.Any())
        {
            yield return null;
            yield break;
        }
        
        foreach (var testDataSourceAttribute in testDataSourceAttributes)
        {
            yield return GetTestDataSourceArguments(type, testDataSourceAttribute);
        }
    }
 
    public object?[]? GetTestDataSourceArguments(Type fallbackType, TestDataSourceAttribute testDataSourceAttribute)
    {
        var classType = testDataSourceAttribute.ClassProvidingDataSource ?? fallbackType;
        var methodName = testDataSourceAttribute.MethodNameProvidingDataSource;
            
        return GetTestDataSourceArguments(classType, methodName);
    }
    
    public object?[]? GetTestDataSourceArguments(
        Type @class,
        string methodName
    )
    { 
        var method = @class.GetMethods().First(x => x.IsStatic && x.Name == methodName);
        
        var result = methodInvoker.InvokeMethod(null, method, BindingFlags.Static | BindingFlags.Public, null).Result;
        
        if (result is null)
        {
            return null;
        }
            
        return [result];
    }
}