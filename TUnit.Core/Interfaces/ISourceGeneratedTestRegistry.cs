namespace TUnit.Core.Interfaces;

/// <summary>
/// Registry for source-generated test factories and invokers.
/// This interface allows the source generator to register AOT-safe execution delegates.
/// </summary>
public interface ISourceGeneratedTestRegistry
{
    /// <summary>
    /// Registers a test class instance factory.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <param name="factory">Factory delegate for creating test instances</param>
    void RegisterClassFactory(string testId, Func<object> factory);

    /// <summary>
    /// Registers a parameterized test class instance factory.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <param name="factory">Factory delegate for creating test instances with arguments</param>
    void RegisterClassFactory(string testId, Func<object?[], object> factory);

    /// <summary>
    /// Registers a test method invoker.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <param name="invoker">Invoker delegate for calling test methods</param>
    void RegisterMethodInvoker(string testId, Func<object, object?[], Task<object?>> invoker);

    /// <summary>
    /// Registers a property setter for dependency injection.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <param name="propertyName">Name of the property to set</param>
    /// <param name="setter">Setter delegate for the property</param>
    void RegisterPropertySetter(string testId, string propertyName, Action<object, object?> setter);

    /// <summary>
    /// Registers pre-resolved data for a test.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <param name="resolvedData">The pre-resolved test data</param>
    void RegisterResolvedData(string testId, CompileTimeResolvedData resolvedData);

    /// <summary>
    /// Gets a registered class factory.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <returns>The class factory if found, null otherwise</returns>
    Func<object>? GetClassFactory(string testId);

    /// <summary>
    /// Gets a registered parameterized class factory.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <returns>The parameterized class factory if found, null otherwise</returns>
    Func<object?[], object>? GetParameterizedClassFactory(string testId);

    /// <summary>
    /// Gets a registered method invoker.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <returns>The method invoker if found, null otherwise</returns>
    Func<object, object?[], Task<object?>>? GetMethodInvoker(string testId);

    /// <summary>
    /// Gets registered property setters for a test.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <returns>Dictionary of property setters</returns>
    IDictionary<string, Action<object, object?>> GetPropertySetters(string testId);

    /// <summary>
    /// Gets pre-resolved data for a test.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <returns>The pre-resolved data if found, null otherwise</returns>
    CompileTimeResolvedData? GetResolvedData(string testId);

    /// <summary>
    /// Gets all registered test IDs.
    /// </summary>
    /// <returns>Collection of all registered test IDs</returns>
    IReadOnlyCollection<string> GetRegisteredTestIds();
}
