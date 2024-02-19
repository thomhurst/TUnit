using System.Collections;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine;

internal class DataSourceRetriever(MethodInvoker methodInvoker)
{
    public IEnumerable<object?> GetTestDataSourceArguments(MethodInfo methodInfo)
    {
        var testDataSourceAttributes = methodInfo.GetCustomAttributes<DataSourceDrivenTestAttribute>().ToList();

        if (!testDataSourceAttributes.Any())
        {
            yield return null;
            yield break;
        }

        var parameterType = methodInfo.GetParameters().FirstOrDefault()?.ParameterType;
        
        foreach (var testDataSourceAttribute in testDataSourceAttributes)
        {
            foreach (var testDataSourceArgument in GetTestDataSourceArguments(fallbackTypeToSearchForMethodIn: methodInfo.DeclaringType!, testDataSourceAttribute, parameterType))
            {
                yield return testDataSourceArgument;
            }
        }
    }
    
    public IEnumerable<object?> GetTestDataSourceArguments(Type type)
    {
        var testDataSourceAttributes = type.GetCustomAttributes<DataSourceDrivenTestAttribute>().ToList();

        if (!testDataSourceAttributes.Any())
        {
            yield return null;
            yield break;
        }
        
        var parameterType = type.GetConstructors().FirstOrDefault()?.GetParameters().FirstOrDefault()?.ParameterType;
        
        foreach (var testDataSourceAttribute in testDataSourceAttributes)
        {
            foreach (var testDataSourceArgument in GetTestDataSourceArguments(type, testDataSourceAttribute, parameterType))
            {
                yield return testDataSourceArgument;
            }
        }
    }
 
    public IEnumerable<object?> GetTestDataSourceArguments(Type fallbackTypeToSearchForMethodIn, DataSourceDrivenTestAttribute dataSourceDrivenTestAttribute, Type? expectedParameterType)
    {
        var classType = dataSourceDrivenTestAttribute.ClassProvidingDataSource ?? fallbackTypeToSearchForMethodIn;
        var methodName = dataSourceDrivenTestAttribute.MethodNameProvidingDataSource;
            
        return GetTestDataSourceArguments(classType, methodName, expectedParameterType);
    }
    
    public IEnumerable<object?> GetTestDataSourceArguments(
        Type @class,
        string methodName, 
        Type? expectedParameterType
    )
    { 
        var method = @class.GetMethods().First(x => x.IsStatic && x.Name == methodName);
        
        var result = methodInvoker.InvokeMethod(null, method, BindingFlags.Static | BindingFlags.Public, null, default).Result;
        
        if (result is null)
        {
            yield return null;
            yield break;
        }

        if (result.GetType().IsAssignableTo(expectedParameterType))
        {
            yield return result;
            yield break;
        }
        
        var enumerableOfParameterType = expectedParameterType != null ? 
            typeof(IEnumerable<>).MakeGenericType(expectedParameterType) 
            : null;

        if (!result.GetType().IsAssignableTo(enumerableOfParameterType))
        {
            yield break;
        }

        foreach (var obj in (IEnumerable) result)
        {
            yield return obj;
        }
    }
}