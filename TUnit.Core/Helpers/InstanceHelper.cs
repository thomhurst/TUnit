using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

[RequiresDynamicCode("InstanceHelper uses reflection and MakeGenericType for dynamic type instantiation")]
[RequiresUnreferencedCode("InstanceHelper uses reflection to create instances and set properties which may require types that aren't statically referenced")]
internal static class InstanceHelper
{
    private static readonly ConcurrentDictionary<(Type type, int constructorHash, int propertyCount), Func<object?[], IDictionary<string, object?>, object>> CachedCreators = new();
    public static object CreateInstance(MethodMetadata methodInformation, object?[]? args, IDictionary<string, object?>? testClassProperties, TestBuilderContext testBuilderContext)
    {
        var classInformation = methodInformation.Class;
        var instance = CreateObject(classInformation, args, testClassProperties, testBuilderContext);

        // Properties with data attributes are handled separately after instance creation
        // to support async initialization

        return instance;
    }

    private static object CreateObject(ClassMetadata classInformation, object?[]? args, IDictionary<string, object?>? testClassProperties, TestBuilderContext testBuilderContext)
    {
        try
        {
            if (classInformation.Attributes.Select(a => a.Instance).OfType<ClassConstructorAttribute>().FirstOrDefault() is { } classConstructorAttribute)
            {
                var classConstructor = (IClassConstructor) Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!;

                return classConstructor.Create(classInformation.Type, new ClassConstructorMetadata
                {
                    TestBuilderContext = testBuilderContext,
                    TestSessionId = TestSessionContext.Current?.Id ?? string.Empty
                });
            }
            var type = classInformation.Type;

            // Find the best matching constructor
            var constructors = type.GetConstructors().Where(x => !x.IsStatic).ToArray();
            var constructor = FindBestMatchingConstructor(constructors);

            var parameters = constructor.GetParameters();

            var castedArgs = args?.Select((a, index) =>
            {
                var parameterType = parameters.ElementAtOrDefault(index)?.ParameterType;

                if (parameterType is null)
                {
                    return a;
                }

                return CastHelper.Cast(parameterType, a);
            }).ToArray();

            if (type.ContainsGenericParameters)
            {
                var substitutedTypes = type.GetGenericArguments()
                    .Select(pc => parameters.Select(p => p.ParameterType).ToList().FindIndex(pt => pt == pc))
                    .Select(i => castedArgs![i]!.GetType())
                    .ToArray();

                type = type.MakeGenericType(substitutedTypes);
            }

            // Check if we have properties to set (including required properties)
            if (testClassProperties?.Count > 0 || HasRequiredProperties(type))
            {
                return CreateInstanceWithProperties(type, constructor, castedArgs, testClassProperties ?? new Dictionary<string, object?>());
            }

            // Fast path: no properties to set, use simple Activator.CreateInstance
            var instance = Activator.CreateInstance(type, castedArgs)!;
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

    private static ConstructorInfo FindBestMatchingConstructor(ConstructorInfo[] constructors)
    {
        return constructors.First();
    }

    private static bool HasRequiredProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(p => p.GetCustomAttribute<RequiredMemberAttribute>() != null || p.GetCustomAttribute(typeof(RequiredMemberAttribute)) != null);
    }

    private static object CreateInstanceWithProperties(Type type, ConstructorInfo constructor, object?[]? castedArgs, IDictionary<string, object?> testClassProperties)
    {
        // Create cache key
        var constructorHash = constructor.GetHashCode();
        var propertyCount = testClassProperties.Count;
        var cacheKey = (type, constructorHash, propertyCount);

        // Try to get cached creator
        if (CachedCreators.TryGetValue(cacheKey, out var cachedCreator))
        {
            return cachedCreator(castedArgs ?? Array.Empty<object?>(), testClassProperties);
        }

        // Build expression tree for object creation with property initialization
        var creator = BuildObjectCreator(type, constructor, testClassProperties.Keys);
        CachedCreators.TryAdd(cacheKey, creator);

        return creator(castedArgs ?? Array.Empty<object?>(), testClassProperties);
    }

    private static Func<object?[], IDictionary<string, object?>, object> BuildObjectCreator(Type type, ConstructorInfo constructor, ICollection<string> propertyNames)
    {
        // Parameters for the lambda
        var ctorArgsParam = Expression.Parameter(typeof(object?[]), "ctorArgs");
        var propsParam = Expression.Parameter(typeof(IDictionary<string, object?>), "props");

        // Constructor arguments
        var ctorParams = constructor.GetParameters();
        var ctorArgExpressions = new Expression[ctorParams.Length];
        for (int i = 0; i < ctorParams.Length; i++)
        {
            var indexExpr = Expression.Constant(i);
            var argExpr = Expression.ArrayIndex(ctorArgsParam, indexExpr);
            ctorArgExpressions[i] = Expression.Convert(argExpr, ctorParams[i].ParameterType);
        }

        // Create new expression
        var newExpr = Expression.New(constructor, ctorArgExpressions);

        // Property bindings
        var bindings = new List<MemberBinding>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Check if property is settable and either required or in the property names
            if (!prop.CanWrite) continue;
            
            var isRequired = prop.GetCustomAttribute<RequiredMemberAttribute>() != null || prop.GetCustomAttribute(typeof(RequiredMemberAttribute)) != null;
            if (!isRequired && !propertyNames.Contains(prop.Name)) continue;

            // Create expression to get value from dictionary
            var propNameExpr = Expression.Constant(prop.Name);
            var hasValueExpr = Expression.Call(propsParam, typeof(IDictionary<string, object?>).GetMethod("ContainsKey", new Type[] { typeof(string) })!, propNameExpr);
            var getValueExpr = Expression.Call(propsParam, typeof(IDictionary<string, object?>).GetMethod("get_Item", new Type[] { typeof(string) })!, propNameExpr);
            var defaultValueExpr = Expression.Default(prop.PropertyType);
            
            // Use conditional to handle missing values
            var valueExpr = Expression.Condition(
                hasValueExpr,
                Expression.Convert(getValueExpr, prop.PropertyType),
                defaultValueExpr
            );

            bindings.Add(Expression.Bind(prop, valueExpr));
        }

        // Create member init expression
        var memberInitExpr = Expression.MemberInit(newExpr, bindings);

        // Wrap in try-catch for better error messages
        var returnLabel = Expression.Label(typeof(object));
        var body = Expression.Block(
            Expression.Return(returnLabel, Expression.Convert(memberInitExpr, typeof(object))),
            Expression.Label(returnLabel, Expression.Default(typeof(object)))
        );

        // Compile lambda
        var lambda = Expression.Lambda<Func<object?[], IDictionary<string, object?>, object>>(
            body,
            ctorArgsParam,
            propsParam
        );

        return lambda.Compile();
    }
}
