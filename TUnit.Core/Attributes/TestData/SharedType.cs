namespace TUnit.Core;

public enum SharedType
{
    None,
    
    PerClass,
    
    [Obsolete("Use PerClass instead.")]
    ForClass = PerClass,
    
    PerAssembly,
    
    [Obsolete("Use PerAssembly instead.")]
    ForAssembly = PerAssembly,
    
    PerTestSession,
    
    [Obsolete("Use PerTestSession instead.")]
    Globally = PerTestSession,
    
    Keyed,
}