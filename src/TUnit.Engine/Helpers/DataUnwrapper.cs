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
        var (unwrapped, metadata) = TestDataRowUnwrapper.UnwrapArray(values,
            values is [ITestDataRow] ? GetTupleDataParameterType(expectedParameters) : null);

        // Then apply type-aware unwrapping
        var data = UnwrapWithTypesInternal(unwrapped, expectedParameters);
        return (data, metadata);
    }

    public static object?[] UnwrapWithTypes(object?[] values, ParameterMetadata[]? expectedParameters)
    {
        // First handle TestDataRow unwrapping
        var (unwrapped, _) = TestDataRowUnwrapper.UnwrapArray(values,
            values is [ITestDataRow] ? GetTupleDataParameterType(expectedParameters) : null);
        return UnwrapWithTypesInternal(unwrapped, expectedParameters);
    }

    private static Type? GetTupleDataParameterType(ParameterMetadata[]? parameters)
    {
        Type? dataType = null;
        if (parameters is null)
        {
            return null;
        }

        foreach (var parameter in parameters)
        {
            if (parameter.Type == typeof(CancellationToken))
            {
                continue;
            }

            if (dataType is not null)
            {
                if (!parameter.IsOptional && !parameter.IsParams)
                {
                    return null;
                }
            }
            else
            {
                dataType = parameter.Type;
            }
        }

        return TupleHelper.IsTupleType(dataType) ? dataType : null;
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

        var lastDataParameter = expectedParameters[lastDataParameterIndex];
        var lastDataParameterType = lastDataParameter.Type;

        // Generated metadata erases a parameter containing type parameters to object,
        // but TypeInfo retains its tuple shape. Preserve all elements until the invoker
        // closes the type; reference tuples and mutable elements need flattened binding.
        if (dataParameterCount == 1
            && DataSourceHelpers.IsErasedGenericTupleParameter(lastDataParameter))
        {
            return DataSourceHelpers.ShouldFlattenErasedTuple(values[0]!, lastDataParameter)
                ? DataSourceHelpers.UnwrapTupleAot(values[0])
                : values;
        }

        // Keep compatible tuples whole (including one-element tuples holding null).
        // Otherwise the invoker needs every element to reconstruct the target tuple
        // with per-element conversions, rather than casting incompatible tuple types.
        if (dataParameterCount == 1 && TupleHelper.IsTupleType(lastDataParameterType))
        {
            // Keep the runtime tuple intact until generic inference closes the parameter type.
            // Singleton value-tuple conversion also needs the outer container: its element
            // can itself be a tuple, indistinguishable from a preserved row after flattening.
            if (!DataSourceHelpers.RequiresFlattenedTupleBinding(values[0]!)
                && (lastDataParameterType.ContainsGenericParameters || lastDataParameterType.IsInstanceOfType(values[0])
                    || lastDataParameterType.IsGenericType && lastDataParameterType.GetGenericTypeDefinition() == typeof(ValueTuple<>)))
            {
                return values;
            }

            // A reference tuple with a lone null must retain its container: [null] also
            // represents an actual null argument. Value tuples always reconstruct from null.
            var tupleElements = DataSourceHelpers.UnwrapTupleAot(values[0]);
            return !lastDataParameterType.IsValueType && tupleElements is [null] ? values : tupleElements;
        }

        // A tuple that is already an instance of the first data parameter's tuple type binds to it
        // whole when every later data parameter can be omitted (optional or params).
        var firstDataParameterType = expectedParameters[firstDataParameterIndex].Type;
        if (dataParameterCount > 1
            && TupleHelper.IsTupleType(firstDataParameterType)
            && (firstDataParameterType.IsInstanceOfType(values[0])
                || values[0] is TupleArgument argument && argument.TupleType == firstDataParameterType)
            && RemainingDataParametersAreOmittable(expectedParameters, firstDataParameterIndex))
        {
            return values[0] is not TupleArgument && DataSourceHelpers.RequiresFlattenedTupleBinding(values[0]!)
                ? [new TupleArgument(values[0]!.GetType(), DataSourceHelpers.UnwrapTupleAot(values[0]))]
                : values;
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
