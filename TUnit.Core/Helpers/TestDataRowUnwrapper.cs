using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Helpers;

/// <summary>
/// Utility for detecting and unwrapping <see cref="TestDataRow{T}"/> instances.
/// </summary>
internal static class TestDataRowUnwrapper
{
    private static readonly Type TestDataRowGenericType = typeof(TestDataRow<>);

    /// <summary>
    /// Checks if the value is a <see cref="TestDataRow{T}"/> and extracts metadata and data.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="data">The extracted data if unwrapped, otherwise the original value.</param>
    /// <param name="metadata">The extracted metadata if unwrapped, otherwise null.</param>
    /// <returns>True if the value was a TestDataRow and was unwrapped.</returns>
    public static bool TryUnwrap(object? value, out object? data, [NotNullWhen(true)] out TestDataRowMetadata? metadata)
    {
        if (value is null)
        {
            data = null;
            metadata = null;
            return false;
        }

        // Use interface-based access for AOT compatibility (avoids reflection)
        if (value is ITestDataRow testDataRow)
        {
            data = testDataRow.GetData();
            metadata = new TestDataRowMetadata(testDataRow.DisplayName, testDataRow.Skip, testDataRow.Categories);
            return true;
        }

        data = value;
        metadata = null;
        return false;
    }

    /// <summary>
    /// Checks if a type is a <see cref="TestDataRow{T}"/>.
    /// </summary>
    public static bool IsTestDataRowType(Type? type)
    {
        return type is not null && type.IsGenericType && type.GetGenericTypeDefinition() == TestDataRowGenericType;
    }

    /// <summary>
    /// Gets the inner data type from a <see cref="TestDataRow{T}"/> type.
    /// </summary>
    public static Type? GetInnerDataType(Type testDataRowType)
    {
        if (!IsTestDataRowType(testDataRowType))
        {
            return null;
        }

        return testDataRowType.GetGenericArguments()[0];
    }

    /// <summary>
    /// Unwraps an array of values, extracting TestDataRow metadata from single-element arrays.
    /// </summary>
    /// <param name="values">The array of values to unwrap.</param>
    /// <returns>A tuple of the unwrapped data array and any extracted metadata.</returns>
    public static (object?[] Data, TestDataRowMetadata? Metadata) UnwrapArray(object?[] values)
    {
        if (values.Length == 1 && TryUnwrap(values[0], out var data, out var metadata))
        {
            // Single TestDataRow<T> - unwrap it
            // If the inner data is already an array, use it directly
            if (data is object?[] dataArray)
            {
                return (dataArray, metadata);
            }

            // Check if the data is a tuple that should be expanded
            if (DataSourceHelpers.IsTuple(data))
            {
                return (data.ToObjectArray(), metadata);
            }

            // Otherwise wrap the single value in an array
            return ([data], metadata);
        }

        return (values, null);
    }
}
