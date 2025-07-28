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
    /// Extracts attributes from method, class, and assembly levels with proper precedence
    /// </summary>
    public static T? GetAttribute<T>(Type testClass, MethodInfo? testMethod = null) where T : Attribute
    {
        // Method level (highest priority)
        if (testMethod != null)
        {
            var methodAttr = testMethod.GetCustomAttribute<T>();
            if (methodAttr != null) return methodAttr;
        }

        // Class level
        var classAttr = testClass.GetCustomAttribute<T>();
        if (classAttr != null) return classAttr;

        // Assembly level (lowest priority)
        return testClass.Assembly.GetCustomAttribute<T>();
    }

    /// <summary>
    /// Extracts all attributes of a specific type from method, class, and assembly levels
    /// </summary>
    public static IEnumerable<T> GetAttributes<T>(Type testClass, MethodInfo? testMethod = null) where T : Attribute
    {
        var attributes = new List<T>();

        // Assembly level (added first, lowest priority)
        attributes.AddRange(testClass.Assembly.GetCustomAttributes<T>());

        // Class level
        attributes.AddRange(testClass.GetCustomAttributes<T>());

        // Method level (added last, highest priority)
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
        
        // Assembly attributes
        attributes.AddRange(testClass.Assembly.GetCustomAttributes());
        
        // Class attributes
        attributes.AddRange(testClass.GetCustomAttributes());
        
        // Method attributes
        attributes.AddRange(testMethod.GetCustomAttributes());
        
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
            // Simply check for IDataSourceAttribute interface
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