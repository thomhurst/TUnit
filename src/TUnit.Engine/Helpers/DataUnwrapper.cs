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

        // Only a single tuple value is spread across the parameters
        if (values.Length != 1 || !DataSourceHelpers.IsTuple(values[0]))
        {
            return values;
        }

        // The engine supplies CancellationToken parameters wherever they are declared, so the row
        // binds to the data parameters only.
        var dataParameterCount = 0;
        var lastDataParameterIndex = -1;
        for (var i = 0; i < expectedParameters.Length; i++)
        {
            if (expectedParameters[i].Type != typeof(CancellationToken))
            {
                dataParameterCount++;
                lastDataParameterIndex = i;
            }
        }

        if (dataParameterCount == 0)
        {
            return DataSourceHelpers.UnwrapTupleAot(values[0]);
        }

        var lastDataParameterType = expectedParameters[lastDataParameterIndex].Type;

        // A single data parameter that expects a tuple receives the tuple itself
        if (dataParameterCount == 1 && TupleHelper.IsTupleType(lastDataParameterType))
        {
            return values;
        }

        var elements = DataSourceHelpers.UnwrapTupleAot(values[0]);

        // A trailing array data parameter collects every remaining element. Otherwise extra
        // elements beyond the data parameters are dropped.
        if (elements.Length <= dataParameterCount || lastDataParameterType is { IsArray: true } && lastDataParameterType.GetArrayRank() == 1)
        {
            return elements;
        }

        var capped = new object?[dataParameterCount];
        Array.Copy(elements, capped, dataParameterCount);
        return capped;
    }
}
