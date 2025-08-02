using System;

namespace TUnit.Core.Attributes;

/// <summary>
/// Indicates that a type or member is compatible with AOT compilation.
/// This attribute serves as documentation and can be used by analyzers to verify AOT compatibility.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor)]
public sealed class AotCompatibleAttribute : Attribute
{
    /// <summary>
    /// Gets or sets an alternative approach for reflection-based scenarios.
    /// </summary>
    public string? AlternativeForReflection { get; set; }
    
    /// <summary>
    /// Gets or sets additional notes about AOT compatibility requirements.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Indicates that a type or member requires reflection and is not AOT-compatible.
/// This attribute helps identify code paths that need special handling in AOT scenarios.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor)]
public sealed class RequiresReflectionAttribute : Attribute
{
    /// <summary>
    /// Gets the reason why reflection is required.
    /// </summary>
    public string Reason { get; }
    
    /// <summary>
    /// Gets or sets an AOT-compatible alternative if available.
    /// </summary>
    public string? AotAlternative { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresReflectionAttribute"/> class.
    /// </summary>
    /// <param name="reason">The reason why reflection is required.</param>
    public RequiresReflectionAttribute(string reason)
    {
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }
}