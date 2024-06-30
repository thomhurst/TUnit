using TUnit.Engine.Models;

namespace TUnit.Engine.Data;

#if !DEBUG
using System.ComponentModel;
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public static class TestDataContainer
{
    public static readonly GetOnlyDictionary<DictionaryTypeTypeKey, object> InjectedSharedPerClassType = new();
    public static readonly GetOnlyDictionary<Type, object> InjectedSharedGlobally = new();
    public static readonly GetOnlyDictionary<DictionaryStringTypeKey, object> InjectedSharedPerKey = new();
}