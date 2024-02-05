using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine;

public class TestDataSourceRetriever(MethodInvoker methodInvoker)
{
    public ParameterArgument[]? GetTestDataSourceArguments(MethodInfo methodInfo,
        TestDataSourceAttribute testDataSourceAttribute, Type[] allClasses)
    {
        // Class name and method name
        var className = testDataSourceAttribute.ClassNameProvidingDataSource ?? methodInfo.DeclaringType!.FullName!;
        var methodName = testDataSourceAttribute.MethodNameProvidingDataSource;
            
        return GetTestDataSourceArguments(className, methodName, allClasses);
    }
    
    public ParameterArgument[]? GetTestDataSourceArguments(
        string className,
        string methodName,
        Type[] allClasses
    )
    {
        var @class = allClasses.FirstOrDefault(x => x.FullName == className)
                     ?? allClasses.First(x => x.Name == className);

        return GetTestDataSourceArguments(@class, methodName);
    }
    
    public ParameterArgument[]? GetTestDataSourceArguments(
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
            
        return [new ParameterArgument(result.GetType(), result)];
    }
}