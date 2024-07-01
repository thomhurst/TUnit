using System.Reflection;

namespace TUnit.Engine.Data;

#if !DEBUG
using System.ComponentModel;
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public static class AttributeCache
{
    public static readonly GetOnlyDictionary<Type, Attribute[]> Types = new();
    public static readonly GetOnlyDictionary<Assembly, Attribute[]> Assemblies = new();
}