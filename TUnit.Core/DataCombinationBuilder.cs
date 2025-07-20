namespace TUnit.Core;

/// <summary>
/// Unified data combination builder that provides consistent logic for both AOT and reflection modes.
/// This ensures identical data expansion behavior regardless of execution mode.
/// </summary>
public static class DataCombinationBuilder
{
    /// <summary>
    /// Builds all data combinations from the provided data sources using cartesian product logic.
    /// Used by both AOT (at compile time) and reflection (at runtime) modes.
    /// </summary>
    #pragma warning disable CS1998 // Async method lacks 'await' operators
    public static async IAsyncEnumerable<TestDataCombination> BuildCombinationsAsync(
        IEnumerable<MethodDataCombination> methodCombinations,
        IEnumerable<ClassDataCombination> classCombinations,
        IEnumerable<PropertyDataCombination> propertyCombinations,
        int repeatCount = 1)
    {
        // Ensure we have at least one combination of each type
        var methodCombs = methodCombinations.ToList();
        var classCombs = classCombinations.ToList();
        var propertyCombs = propertyCombinations.ToList();

        if (!methodCombs.Any())
        {
            methodCombs.Add(new MethodDataCombination());
        }
        if (!classCombs.Any())
        {
            classCombs.Add(new ClassDataCombination());
        }
        if (!propertyCombs.Any())
        {
            propertyCombs.Add(new PropertyDataCombination());
        }

        // Generate cartesian product
        foreach (var methodCombination in methodCombs)
        {
            foreach (var classCombination in classCombs)
            {
                foreach (var propertyCombination in propertyCombs)
                {
                    for (int repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
                    {
                        yield return new TestDataCombination
                        {
                            MethodDataFactories = methodCombination.DataFactories,
                            ClassDataFactories = classCombination.DataFactories,
                            MethodDataSourceIndex = methodCombination.DataSourceIndex,
                            MethodLoopIndex = methodCombination.LoopIndex,
                            ClassDataSourceIndex = classCombination.DataSourceIndex,
                            ClassLoopIndex = classCombination.LoopIndex,
                            RepeatIndex = repeatIndex
                        };
                    }
                }
            }
        }
    }
    #pragma warning restore CS1998

    /// <summary>
    /// Wraps exception handling around data combination generation.
    /// Returns combinations with DataGenerationException set on failure.
    /// </summary>
    public static async IAsyncEnumerable<TestDataCombination> BuildCombinationsWithErrorHandlingAsync(
        Func<IAsyncEnumerable<TestDataCombination>> generator)
    {
        var enumerator = generator().GetAsyncEnumerator();
        var yielded = new List<TestDataCombination>();
        TestDataCombination? errorCombination = null;
        
        try
        {
            while (await enumerator.MoveNextAsync())
            {
                yielded.Add(enumerator.Current);
            }
        }
        catch (Exception ex)
        {
            // Store error for yielding after try-catch
            errorCombination = new TestDataCombination
            {
                DataGenerationException = ex,
                DisplayName = $"[DATA GENERATION ERROR: {ex.Message}]"
            };
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
        
        // Yield all collected combinations
        foreach (var combination in yielded)
        {
            yield return combination;
        }
        
        // Yield error combination if there was an exception
        if (errorCombination != null)
        {
            yield return errorCombination;
        }
    }
}

/// <summary>
/// Represents a single method data combination with its factories and indices.
/// </summary>
public sealed class MethodDataCombination
{
    public Func<Task<object?>>[] DataFactories { get; init; } = [
    ];
    public int DataSourceIndex { get; init; }
    public int LoopIndex { get; init; }
}

/// <summary>
/// Represents a single class data combination with its factories and indices.
/// </summary>
public sealed class ClassDataCombination
{
    public Func<Task<object?>>[] DataFactories { get; init; } = [
    ];
    public int DataSourceIndex { get; init; }
    public int LoopIndex { get; init; }
}

/// <summary>
/// Represents property data combinations.
/// </summary>
public sealed class PropertyDataCombination
{
    // Properties are now resolved directly via PropertyDataSources in PropertyInjector
}