using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

[RequiresDynamicCode("Calls System.Type.MakeGenericType(params Type[])")]
[RequiresUnreferencedCode("Reflection")]
internal static class InstanceHelper
{
    public static object CreateInstance(SourceGeneratedClassInformation classInformation, object?[]? args, IDictionary<string, object?>? testClassProperties, TestBuilderContext testBuilderContext)
    {
        try
        {
            if (classInformation.Attributes.OfType<ClassConstructorAttribute>().FirstOrDefault() is { } classConstructorAttribute)
            {
                var classConstructor = (IClassConstructor) Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!;
                
                return classConstructor.Create(classInformation.Type, new ClassConstructorMetadata
                {
                    TestBuilderContext = testBuilderContext,
                    TestSessionId = TestSessionContext.Current?.Id ?? string.Empty
                });
            }
            var type = classInformation.Type;
            
            var parameters = type.GetConstructors().First(x => !x.IsStatic).GetParameters();

            var castedArgs = args?.Select((a, index) => CastHelper.Cast(parameters[index].ParameterType, a)).ToArray();

            if (type.ContainsGenericParameters)
            {
                var substitutedTypes = type.GetGenericArguments()
                    .Select(pc => parameters.Select(p => p.ParameterType).ToList().FindIndex(pt => pt == pc))
                    .Select(i => castedArgs![i]!.GetType())
                    .ToArray();
                
                type = type.MakeGenericType(substitutedTypes);
            }
            
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
            throw new TUnitException("Cannot create instance of type " + classInformation.Type.FullName, e);
        }
    }
}