using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Exceptions;

namespace TUnit.Core.Helpers;

[RequiresUnreferencedCode("Reflection")]
internal static class InstanceHelper
{
    public static object CreateInstance([DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type type, object?[]? args, IDictionary<string, object?>? testClassProperties)
    {
        try
        {
            var parameters = type.GetConstructors().First(x => !x.IsStatic).GetParameters();

            var castedArgs = args?.Select((a, index) => CastHelper.Cast(parameters[index].ParameterType, a)).ToArray();

            var instance = Activator.CreateInstance(type, castedArgs)!;
            
            foreach (var (propertyName, value) in testClassProperties ?? new Dictionary<string, object?>())
            {
                type.GetProperty(propertyName)!.SetValue(instance, value);
            }
            
            return instance;
        }
        catch (TargetInvocationException targetInvocationException)
        {
            ExceptionDispatchInfo.Capture(targetInvocationException.InnerException ?? targetInvocationException).Throw();
            throw;
        }
        catch (MissingMethodException e)
        {
            throw new TUnitException("Cannot create instance of type " + type.FullName, e);
        }
    }
}