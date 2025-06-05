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
            // Handle Tuple recursively
            var result = new List<object?>();
            FlattenTuple(tuple, result);
            objectArray = result.ToArray();

            return true;
        }

        if (type.IsValueType && type.FullName!.StartsWith("System.ValueTuple"))
        {
            // Handle ValueTuple recursively
            var result = new List<object?>();
            FlattenValueTuple(tuple, result);
            objectArray = result.ToArray();

            return true;
        }

        return false;
    }

    private static void FlattenTuple(object tuple, List<object?> result)
    {
        var type = tuple.GetType();

        var properties = type.GetProperties()
            .Where(p => p.Name.StartsWith("Item"))
            .OrderBy(p => p.Name)
            .ToList();

        // Process items 1 to 7 (or fewer if the tuple is smaller)
        for (var i = 0; i < properties.Count - 1; i++)
        {
            result.Add(properties[i].GetValue(tuple));
        }

        // Check if we have a Rest property (8th item in a large tuple)
        if (properties.Count == 8)
        {
            var rest = properties[7].GetValue(tuple);

            if (rest != null && rest.GetType().FullName!.StartsWith("System.Tuple"))
            {
                FlattenTuple(rest, result);
            }
            else
            {
                result.Add(rest);
            }
        }
    }

    private static void FlattenValueTuple(object tuple, List<object?> result)
    {
        var type = tuple.GetType();

        var fields = type.GetFields()
            .OrderBy(f => f.Name)
            .ToList();

        // Process items 1 to 7 (or fewer if the tuple is smaller)
        for (var i = 0; i < fields.Count; i++)
        {
            // Skip the last field if it's called Rest
            if (i == 7 || (i == fields.Count - 1 && fields[i].Name == "Rest"))
            {
                var rest = fields[i].GetValue(tuple);

                if (rest != null && rest.GetType().IsValueType && rest.GetType().FullName!.StartsWith("System.ValueTuple"))
                {
                    FlattenValueTuple(rest, result);
                }
                else
                {
                    result.Add(rest);
                }

                break;
            }

            result.Add(fields[i].GetValue(tuple));
        }
    }
}

