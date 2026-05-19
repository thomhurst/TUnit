namespace TUnit.Core;

/// <summary>
/// Excludes an assembly from TUnit test discovery.
/// </summary>
/// <remarks>
/// Apply this attribute at assembly level when an assembly references TUnit infrastructure but
/// should not contribute tests or hooks to a consuming test run.
/// </remarks>
/// <example>
/// <code>
/// using TUnit.Core;
///
/// [assembly: ExcludeFromTestDiscovery]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class ExcludeFromTestDiscoveryAttribute : Attribute;
