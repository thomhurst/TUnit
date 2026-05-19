using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Discovery;

internal static class AssemblyDiscoveryFilter
{
    public static bool IsExcludedFromTestDiscovery(Assembly assembly)
    {
        try
        {
            foreach (var attribute in CustomAttributeData.GetCustomAttributes(assembly))
            {
                var attributeType = attribute.AttributeType;
                if (attributeType == typeof(ExcludeFromTestDiscoveryAttribute) ||
                    attributeType.FullName == typeof(ExcludeFromTestDiscoveryAttribute).FullName)
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
