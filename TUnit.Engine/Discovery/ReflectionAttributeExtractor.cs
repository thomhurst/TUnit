using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Extracts attributes from test classes and methods following the inheritance hierarchy in reflection mode
/// </summary>
internal static class ReflectionAttributeExtractor
{
    /// <summary>
    /// Cache for attribute lookups to avoid repeated reflection calls
    /// </summary>
    private static readonly ConcurrentDictionary<AttributeCacheKey, Attribute?> _attributeCache = new();

    /// <summary>
    /// Composite cache key combining type, method, and attribute type information
    /// </summary>
    private readonly struct AttributeCacheKey : IEquatable<AttributeCacheKey>
    {
        public readonly Type TestClass;
        public readonly MethodInfo? TestMethod;
        public readonly Type AttributeType;

        public AttributeCacheKey(Type testClass, MethodInfo? testMethod, Type attributeType)
        {
            TestClass = testClass;
            TestMethod = testMethod;
            AttributeType = attributeType;
        }

        public bool Equals(AttributeCacheKey other)
        {
            return TestClass == other.TestClass &&
                   TestMethod == other.TestMethod &&
                   AttributeType == other.AttributeType;
        }

        public override bool Equals(object? obj)
        {
            return obj is AttributeCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = TestClass.GetHashCode();
                hash = (hash * 397) ^ (TestMethod?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ AttributeType.GetHashCode();
                return hash;
            }
        }
    }
    /// <summary>
    /// Extracts attributes from method, class, and assembly levels with proper precedence
    /// </summary>
    public static T? GetAttribute<T>(Type testClass, MethodInfo? testMethod = null) where T : Attribute
    {
        var cacheKey = new AttributeCacheKey(testClass, testMethod, typeof(T));
        
        return (T?)_attributeCache.GetOrAdd(cacheKey, key =>
        {
            // Original lookup logic preserved
            if (key.TestMethod != null)
            {
                var methodAttr = key.TestMethod.GetCustomAttribute<T>();
                if (methodAttr != null)
                {
                    return methodAttr;
                }
            }

            var classAttr = key.TestClass.GetCustomAttribute<T>();
            if (classAttr != null)
            {
                return classAttr;
            }

            return key.TestClass.Assembly.GetCustomAttribute<T>();
        });
    }

    /// <summary>
    /// Extracts all attributes of a specific type from method, class, and assembly levels
    /// </summary>
    public static IEnumerable<T> GetAttributes<T>(Type testClass, MethodInfo? testMethod = null) where T : Attribute
    {
        var attributes = new List<T>();

        attributes.AddRange(testClass.Assembly.GetCustomAttributes<T>());
        attributes.AddRange(testClass.GetCustomAttributes<T>());

        if (testMethod != null)
        {
            attributes.AddRange(testMethod.GetCustomAttributes<T>());
        }

        return attributes;
    }

    public static string[] ExtractCategories(Type testClass, MethodInfo testMethod)
    {
        var categories = new HashSet<string>();
        
        foreach (var attr in GetAttributes<CategoryAttribute>(testClass, testMethod))
        {
            categories.Add(attr.Category);
        }

        return categories.ToArray();
    }

    public static bool IsTestSkipped(Type testClass, MethodInfo testMethod, out string? skipReason)
    {
        var skipAttr = GetAttribute<SkipAttribute>(testClass, testMethod);
        skipReason = skipAttr?.Reason;
        return skipAttr != null;
    }

    public static int? ExtractTimeout(Type testClass, MethodInfo testMethod)
    {
        var timeoutAttr = GetAttribute<TimeoutAttribute>(testClass, testMethod);
        return timeoutAttr != null ? (int)timeoutAttr.Timeout.TotalMilliseconds : null;
    }

    public static int ExtractRetryCount(Type testClass, MethodInfo testMethod)
    {
        var retryAttr = GetAttribute<RetryAttribute>(testClass, testMethod);
        return retryAttr?.Times ?? 0;
    }

    public static int ExtractRepeatCount(Type testClass, MethodInfo testMethod)
    {
        var repeatAttr = GetAttribute<RepeatAttribute>(testClass, testMethod);
        return repeatAttr?.Times ?? 0;
    }

    public static bool CanRunInParallel(Type testClass, MethodInfo testMethod)
    {
        return GetAttribute<NotInParallelAttribute>(testClass, testMethod) == null;
    }

    public static TestDependency[] ExtractDependencies(Type testClass, MethodInfo testMethod)
    {
        var dependencies = new List<TestDependency>();
        
        foreach (var attr in GetAttributes<DependsOnAttribute>(testClass, testMethod))
        {
            dependencies.Add(attr.ToTestDependency());
        }

        return dependencies.ToArray();
    }

    public static IDataSourceAttribute[] ExtractDataSources(ICustomAttributeProvider attributeProvider)
    {
        var dataSources = new List<IDataSourceAttribute>();

        foreach (var attr in attributeProvider.GetCustomAttributes(inherit: false))
        {
            if (attr is IDataSourceAttribute dataSourceAttr)
            {
                dataSources.Add(dataSourceAttr);
            }
        }

        return dataSources.ToArray();
    }

    public static Attribute[] GetAllAttributes(Type testClass, MethodInfo testMethod)
    {
        var attributes = new List<Attribute>();
        
        // Add in reverse order of precedence so method attributes come first
        // This ensures ScopedAttributeFilter will keep method-level attributes over class/assembly
        attributes.AddRange(testMethod.GetCustomAttributes());
        attributes.AddRange(testClass.GetCustomAttributes());
        attributes.AddRange(testClass.Assembly.GetCustomAttributes());
        
        return attributes.ToArray();
    }

    [UnconditionalSuppressMessage("Trimming",
        "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicProperties' in call to 'System.Type.GetProperties(BindingFlags)'",
        Justification = "Reflection mode requires dynamic access")]
    public static PropertyDataSource[] ExtractPropertyDataSources([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type testClass)
    {
        var propertyDataSources = new List<PropertyDataSource>();

        var properties = testClass.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(p => p.CanWrite);

        foreach (var property in properties)
        {
            foreach (var attr in property.GetCustomAttributes())
            {
                if (attr is IDataSourceAttribute dataSourceAttr)
                {
                    propertyDataSources.Add(new PropertyDataSource
                    {
                        PropertyName = property.Name,
                        PropertyType = property.PropertyType,
                        DataSource = dataSourceAttr
                    });
                }
            }
        }

        return propertyDataSources.ToArray();
    }
}