namespace TUnit.Core;

/// <summary>
/// Excludes assemblies from TUnit test discovery.
/// </summary>
/// <remarks>
/// Apply this attribute to the entry test assembly to exclude referenced assemblies that should
/// not contribute tests or hooks to that run. Use a marker type from each assembly to exclude.
/// The parameterless form excludes the assembly it is applied to.
/// </remarks>
/// <example>
/// <code>
/// using TUnit.Core;
///
/// [assembly: ExcludeFromTestDiscovery(typeof(SharedTestProject.Marker))]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class ExcludeFromTestDiscoveryAttribute : Attribute
{
    /// <summary>
    /// Excludes the assembly this attribute is applied to from test discovery.
    /// </summary>
    public ExcludeFromTestDiscoveryAttribute()
    {
        AssemblyMarkerTypes = [];
    }

    /// <summary>
    /// Excludes the assemblies containing the supplied marker types from test discovery.
    /// </summary>
    /// <param name="assemblyMarkerTypes">Types from assemblies to exclude.</param>
    public ExcludeFromTestDiscoveryAttribute(params Type[] assemblyMarkerTypes)
    {
        AssemblyMarkerTypes = assemblyMarkerTypes;
    }

    /// <summary>
    /// Types from assemblies to exclude from test discovery.
    /// </summary>
    public Type[] AssemblyMarkerTypes { get; }
}
