using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Helper for creating test instances with proper data source handling
/// </summary>
internal static class TestInstanceHelper
{
    /// <summary>
    /// Creates a test instance with data from class data sources
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2067:Target type's member does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    public static object? CreateTestInstanceWithData(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClass,
        IDataSourceAttribute[]? classDataSources)
    {
        try
        {
            if (testClass.IsAbstract)
            {
                return null;
            }
            
            var inheritsTests = testClass.GetCustomAttribute<InheritsTestsAttribute>() != null;
            if (inheritsTests)
            {
                return CreateInheritedTestInstance(testClass);
            }
            
            // Get all constructors
            var constructors = testClass.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            // First check for [TestConstructor] attribute
            var testConstructorMarked = constructors.Where(c => c.GetCustomAttribute<TestConstructorAttribute>() != null).ToArray();
            
            // If we have class data sources, try to match them to constructor parameters
            if (classDataSources is { Length: > 0 })
            {
                // Get data from class data sources
                var classData = GetFirstDataFromSources(classDataSources);
                
                if (classData is { Length: > 0 })
                {
                    // If we have a [TestConstructor] marked constructor, prefer it if it matches data count
                    if (testConstructorMarked.Length > 0)
                    {
                        var markedConstructor = testConstructorMarked[0];
                        if (markedConstructor.GetParameters().Length == classData.Length)
                        {
                            return markedConstructor.Invoke(classData);
                        }
                    }
                    
                    // Find constructor that matches the data count
                    var matchingConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length == classData.Length);
                    
                    if (matchingConstructor != null)
                    {
                        return matchingConstructor.Invoke(classData);
                    }
                }
            }
            
            // If we have a marked constructor but no data sources, use it directly
            if (testConstructorMarked.Length > 0)
            {
                var markedConstructor = testConstructorMarked[0];
                return ConstructorHelper.CreateTestClassInstanceWithConstructor(testClass, markedConstructor);
            }
            
            // Fall back to parameterless constructor or GenericTestHelper
            return GenericTestHelper.CreateTestClassInstance(testClass);
        }
        catch (Exception ex)
        {
            // During discovery, we might not have actual data yet, so fall back to default creation
            try
            {
                return GenericTestHelper.CreateTestClassInstance(testClass);
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Cannot create instance of '{testClass.FullName}' for test discovery. " +
                    $"Original error: {ex.Message}", ex);
            }
        }
    }
    
    /// <summary>
    /// Gets the first set of data from test data sources (for discovery purposes)
    /// </summary>
    private static object?[]? GetFirstDataFromSources(IDataSourceAttribute[] dataSources)
    {
        if (dataSources == null || dataSources.Length == 0)
        {
            return null;
        }
        
        var dataList = new List<object?>();
        
        foreach (var dataSource in dataSources)
        {
            try
            {
                // Get first data item from the source
                var data = GetFirstDataFromSource(dataSource);
                if (data != null)
                {
                    dataList.AddRange(data);
                }
            }
            catch
            {
                // Ignore errors during discovery
            }
        }
        
        return dataList.Count > 0 ? dataList.ToArray() : null;
    }
    
    private static object?[]? GetFirstDataFromSource(IDataSourceAttribute dataSource)
    {
        // For discovery, we can't evaluate data sources that might have complex dependencies
        // Just return null to indicate we should use default construction
        return null;
    }
    
    /// <summary>
    /// Creates test instance for inherited test classes
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    public static object? CreateInheritedTestInstance(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClass)
    {
        try
        {
            // For inherited classes with required properties from base class,
            // we cannot create an instance during discovery.
            // This matches the behavior of the source generator which handles this at compile time.
            
            var currentType = testClass;
            while (currentType != null && currentType != typeof(object))
            {
                if (currentType.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() != null)
                {
                    // For discovery purposes, we'll return a special marker instance
                    // The actual instance will be created during test execution with proper data
                    return new DiscoveryPlaceholderInstance();
                }
                currentType = currentType.BaseType;
            }
            
            // For other cases, use the normal creation path
            var instance = GenericTestHelper.CreateTestClassInstance(testClass);
            
            // If the class has required properties from the base class, try to initialize them
            if (instance != null)
            {
                InitializeInheritedRequiredProperties(instance, testClass);
            }
            
            return instance;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Cannot create instance of inherited test class '{testClass.FullName}'. " +
                $"Error: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Placeholder instance used during discovery when we can't create a real instance
    /// </summary>
    private sealed class DiscoveryPlaceholderInstance
    {
        public override string ToString() => "<discovery-placeholder>";
    }
    
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2067:Target type's member does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target method return value does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    private static void InitializeInheritedRequiredProperties(object instance, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var currentType = type;
        while (currentType != null && currentType != typeof(object))
        {
            ConstructorHelper.InitializeRequiredProperties(instance, currentType);
            currentType = currentType.BaseType;
        }
    }
}