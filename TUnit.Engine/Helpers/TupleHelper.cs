using System.Diagnostics.CodeAnalysis;

namespace TUnit.Engine.Helpers;

[UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
internal class TupleHelper
{
    public static bool TryParseTupleToObjectArray(object? tuple, [NotNullWhen(true)] out object?[]? objectArray)
    {
        objectArray = null;
        
        if (tuple == null)
        {
            return false;
        }

        var type = tuple.GetType();

        if (type.IsGenericType && type.FullName!.StartsWith("System.Tuple"))
        {
            // Handle Tuple
            objectArray = type.GetProperties()
                .Where(p => p.Name.StartsWith("Item"))
                .OrderBy(p => p.Name)
                .Select(p => p.GetValue(tuple))
                .ToArray();
            
            return true;
        }
        
        if (type.IsValueType && type.FullName!.StartsWith("System.ValueTuple"))
        {
            // Handle ValueTuple
            objectArray = type.GetFields()
                .OrderBy(f => f.Name)
                .Select(f => f.GetValue(tuple))
                .ToArray();

            return true;
        }

        return false;
    }
}