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
        var firstDataParameterIndex = -1;
        var lastDataParameterIndex = -1;
        for (var i = 0; i < expectedParameters.Length; i++)
        {
            if (expectedParameters[i].Type != typeof(CancellationToken))
            {
                if (dataParameterCount == 0)
                {
                    firstDataParameterIndex = i;
                }

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

        // A tuple that is already an instance of the first data parameter's tuple type binds to it
        // whole when every later data parameter can be omitted (optional or params).
        var firstDataParameterType = expectedParameters[firstDataParameterIndex].Type;
        if (dataParameterCount > 1
            && TupleHelper.IsTupleType(firstDataParameterType)
            && firstDataParameterType.IsInstanceOfType(values[0])
            && RemainingDataParametersAreOmittable(expectedParameters, firstDataParameterIndex))
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

    private static bool RemainingDataParametersAreOmittable(ParameterMetadata[] parameters, int firstDataParameterIndex)
    {
        for (var i = firstDataParameterIndex + 1; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (parameter.Type != typeof(CancellationToken) && !parameter.IsOptional && !parameter.IsParams)
            {
                return false;
            }
        }

        return true;
    }
}
