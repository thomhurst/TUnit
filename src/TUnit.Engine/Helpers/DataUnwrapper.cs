using System;
using TUnit.Core;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Helpers;

internal class DataUnwrapper
{
    /// <summary>
    /// Unwraps values, handling tuples and TestDataRow wrappers.
    /// </summary>
    public static object?[] Unwrap(object?[] values)
    {
        // First check for TestDataRow wrapper
        var (unwrapped, _) = TestDataRowUnwrapper.UnwrapArray(values);

        // Then handle tuple unwrapping
        if (unwrapped.Length == 1 && DataSourceHelpers.IsTuple(unwrapped[0]))
        {
            return unwrapped[0].ToObjectArray();
        }

        return unwrapped;
    }

    /// <summary>
    /// Unwraps values and extracts any TestDataRow metadata.
    /// </summary>
    public static (object?[] Data, TestDataRowMetadata? Metadata) UnwrapWithMetadata(object?[] values)
    {
        // First check for TestDataRow wrapper
        var (unwrapped, metadata) = TestDataRowUnwrapper.UnwrapArray(values);

        // Then handle tuple unwrapping
        if (unwrapped.Length == 1 && DataSourceHelpers.IsTuple(unwrapped[0]))
        {
            return (unwrapped[0].ToObjectArray(), metadata);
        }

        return (unwrapped, metadata);
    }

    /// <summary>
    /// Unwraps values with type information and extracts any TestDataRow metadata.
    /// </summary>
    public static (object?[] Data, TestDataRowMetadata? Metadata) UnwrapWithTypesAndMetadata(
        object?[] values,
        ParameterMetadata[]? expectedParameters)
    {
        // First check for TestDataRow wrapper
        var (unwrapped, metadata) = TestDataRowUnwrapper.UnwrapArray(values);

        // Then apply type-aware unwrapping
        var data = UnwrapWithTypesInternal(unwrapped, expectedParameters);
        return (data, metadata);
    }

    public static object?[] UnwrapWithTypes(object?[] values, ParameterMetadata[]? expectedParameters)
    {
        // First handle TestDataRow unwrapping
        var (unwrapped, _) = TestDataRowUnwrapper.UnwrapArray(values);
        return UnwrapWithTypesInternal(unwrapped, expectedParameters);
    }

    private static object?[] UnwrapWithTypesInternal(object?[] values, ParameterMetadata[]? expectedParameters)
    {
        // If no parameter information, fall back to default behavior
        if (expectedParameters == null || expectedParameters.Length == 0)
        {
            if (values.Length == 1 && DataSourceHelpers.IsTuple(values[0]))
            {
                return values[0].ToObjectArray();
            }

            return values;
        }

        // Special case: If we have a single value that's a tuple, and a single parameter that expects a tuple,
        // don't unwrap it
        if (values.Length == 1 &&
            expectedParameters.Length == 1 &&
            DataSourceHelpers.IsTuple(values[0]) &&
            TupleHelper.IsTupleType(expectedParameters[0].Type))
        {
            return values;
        }

        // Otherwise use the default unwrapping
        if (values.Length == 1 && DataSourceHelpers.IsTuple(values[0]))
        {
            var paramTypes = new Type[expectedParameters.Length];
            for (var i = 0; i < expectedParameters.Length; i++)
            {
                paramTypes[i] = expectedParameters[i].Type;
            }

            return values[0].ToObjectArrayWithTypes(paramTypes);
        }

        return values;
    }
}
