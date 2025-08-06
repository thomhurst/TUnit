using TUnit.Core.Helpers;

namespace TUnit.Engine.Helpers;

public class DataUnwrapper
{
    public static object?[] Unwrap(object?[] values)
    {
        if(values.Length == 1 && DataSourceHelpers.IsTuple(values[0]))
        {
            return values[0].ToObjectArray();
        }

        return values;
    }
}
