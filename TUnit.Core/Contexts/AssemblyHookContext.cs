using System;
using System.Reflection;

namespace TUnit.Core.Contexts;

/// <summary>
/// Context used for assembly-level hooks (BeforeAssembly, AfterAssembly)
/// </summary>
public sealed class AssemblyHookContext : ContextBase
{
    /// <summary>
    /// The assembly being tested
    /// </summary>
    public Assembly Assembly { get; }
    
    /// <summary>
    /// Name of the assembly
    /// </summary>
    public string AssemblyName => Assembly.GetName().Name ?? "Unknown";
    
    public AssemblyHookContext(Assembly assembly)
    {
        Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    }
}