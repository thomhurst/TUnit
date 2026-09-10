namespace TUnit.Core.Helpers;

/// <summary>
/// Computes Cartesian products for data source expansion
/// (e.g. <see cref="MatrixDataSourceAttribute"/> and <see cref="CombinedDataSourcesAttribute"/>).
/// </summary>
internal static class CartesianProductHelper
{
    /// <summary>
    /// Computes the Cartesian product of the given sets.
    /// The last dimension varies fastest, matching Aggregate/SelectMany ordering.
    /// </summary>
    public static IEnumerable<T[]> GetCartesianProduct<T>(IReadOnlyList<IReadOnlyList<T>> sets)
    {
        var dimensionCount = sets.Count;

        // Any empty dimension makes the product empty (matches the previous
        // Aggregate/SelectMany behaviour where SelectMany over [] yields nothing).
        for (var dimension = 0; dimension < dimensionCount; dimension++)
        {
            if (sets[dimension].Count == 0)
            {
                yield break;
            }
        }

        // Odometer-style Cartesian product: the last dimension varies fastest,
        // matching the previous Aggregate/SelectMany ordering exactly.
        var indices = new int[dimensionCount];

        while (true)
        {
            var row = new T[dimensionCount];
            for (var dimension = 0; dimension < dimensionCount; dimension++)
            {
                row[dimension] = sets[dimension][indices[dimension]];
            }

            yield return row;

            // Advance the odometer from the rightmost dimension.
            var position = dimensionCount - 1;
            while (position >= 0)
            {
                if (++indices[position] < sets[position].Count)
                {
                    break;
                }

                indices[position] = 0;
                position--;
            }

            // All dimensions wrapped back to zero: enumeration is complete.
            if (position < 0)
            {
                yield break;
            }
        }
    }
}
