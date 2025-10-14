namespace TUnit.Assertions.Tests.Bugs;

public class Tests3367
{
    /// <summary>
    /// Custom comparer for doubles with tolerance.
    /// Note: This comparer intentionally does NOT implement GetHashCode correctly
    /// for tolerance-based equality, which is extremely difficult to do correctly.
    /// TUnit should handle this gracefully.
    /// </summary>
    public class DoubleComparer(double tolerance) : IEqualityComparer<double>
    {
        private readonly double _tolerance = tolerance;

        public bool Equals(double x, double y) => Math.Abs(x - y) <= _tolerance;

        public int GetHashCode(double obj) => obj.GetHashCode();
    }

    [Test]
    public async Task IsEquivalentTo_WithCustomComparer_SingleElement_ShouldSucceed()
    {
        // Arrange
        var comparer = new DoubleComparer(0.0001);
        double value1 = 0.29999999999999999;
        double value2 = 0.30000000000000004;

        // Act & Assert - single element comparison works
        await TUnitAssert.That(comparer.Equals(value1, value2)).IsTrue();
    }

    [Test]
    public async Task IsEquivalentTo_WithCustomComparer_Array_ShouldSucceed()
    {
        // Arrange
        var comparer = new DoubleComparer(0.0001);
        double[] array1 = [0.1, 0.2, 0.29999999999999999];
        double[] array2 = [0.1, 0.2, 0.30000000000000004];

        // Act & Assert - array comparison should work with custom comparer
        await TUnitAssert.That(array1).IsEquivalentTo(array2).Using(comparer);
    }

    [Test]
    public async Task IsEquivalentTo_WithCustomComparer_Array_DifferentOrder_ShouldSucceed()
    {
        // Arrange
        var comparer = new DoubleComparer(0.0001);
        double[] array1 = [0.1, 0.29999999999999999, 0.2];
        double[] array2 = [0.2, 0.1, 0.30000000000000004];

        // Act & Assert - should work regardless of order
        await TUnitAssert.That(array1).IsEquivalentTo(array2).Using(comparer);
    }

    [Test]
    public async Task IsEquivalentTo_WithCustomComparer_Array_NotEquivalent_ShouldFail()
    {
        // Arrange
        var comparer = new DoubleComparer(0.0001);
        double[] array1 = [0.1, 0.2, 0.3];
        double[] array2 = [0.1, 0.2, 0.5]; // 0.5 is not within tolerance of 0.3

        // Act & Assert - should fail when values are not within tolerance
        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(
            async () => await TUnitAssert.That(array1).IsEquivalentTo(array2).Using(comparer));

        await TUnitAssert.That(exception).IsNotNull();
    }

    [Test]
    public async Task IsEquivalentTo_WithCustomComparer_DuplicateValues_ShouldSucceed()
    {
        // Arrange
        var comparer = new DoubleComparer(0.0001);
        double[] array1 = [0.1, 0.2, 0.2, 0.3];
        double[] array2 = [0.1, 0.20000000000000001, 0.19999999999999999, 0.30000000000000004];

        // Act & Assert - should handle duplicates correctly
        await TUnitAssert.That(array1).IsEquivalentTo(array2).Using(comparer);
    }
}
