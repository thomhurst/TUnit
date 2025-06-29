using System.Reflection;

namespace TUnit.Core;

/// <summary>
/// Context object providing information about where data is being requested from
/// </summary>
public sealed class DataSourceContext
{
    /// <summary>
    /// The type containing the test (for all levels)
    /// </summary>
    public Type TestClassType { get; }

    /// <summary>
    /// The member being targeted (TypeInfo for class, MethodInfo for method, PropertyInfo for property)
    /// </summary>
    public MemberInfo? TargetMember { get; }

    /// <summary>
    /// The parameter being targeted (null if not parameter-level)
    /// </summary>
    public ParameterInfo? TargetParameter { get; }

    /// <summary>
    /// The level at which the data source is being applied
    /// </summary>
    public DataSourceLevel Level { get; }

    /// <summary>
    /// Attributes on the target member or parameter
    /// </summary>
    public IReadOnlyList<Attribute> Attributes { get; }

    /// <summary>
    /// Service provider for dependency resolution (optional)
    /// </summary>
    public IServiceProvider? ServiceProvider { get; }

    public DataSourceContext(
        Type testClassType,
        DataSourceLevel level,
        MemberInfo? targetMember = null,
        ParameterInfo? targetParameter = null,
        IReadOnlyList<Attribute>? attributes = null,
        IServiceProvider? serviceProvider = null)
    {
        TestClassType = testClassType ?? throw new ArgumentNullException(nameof(testClassType));
        Level = level;
        TargetMember = targetMember;
        TargetParameter = targetParameter;
        Attributes = attributes ?? Array.Empty<Attribute>();
        ServiceProvider = serviceProvider;
    }
}

/// <summary>
/// Indicates the level at which a data source is being applied
/// </summary>
public enum DataSourceLevel
{
    /// <summary>
    /// Data source applied at the class level (constructor parameters)
    /// </summary>
    Class,

    /// <summary>
    /// Data source applied at the method level (test method parameters)
    /// </summary>
    Method,

    /// <summary>
    /// Data source applied to a property
    /// </summary>
    Property,

    /// <summary>
    /// Data source applied to a specific parameter
    /// </summary>
    Parameter
}
