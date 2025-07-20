namespace TUnit.UnitTests;

/// <summary>
/// Tests for TypeArrayComparer functionality
/// </summary>
public class TypeArrayComparerTests
{
    [Test]
    public async Task TypeArrayComparer_EqualArrays_ReturnsTrue()
    {
        // Arrange
        var comparer = TypeArrayComparer.Instance;
        var array1 = new[] { typeof(int), typeof(string), typeof(bool) };
        var array2 = new[] { typeof(int), typeof(string), typeof(bool) };

        // Act
        var result = comparer.Equals(array1, array2);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task TypeArrayComparer_DifferentArrays_ReturnsFalse()
    {
        // Arrange
        var comparer = TypeArrayComparer.Instance;
        var array1 = new[] { typeof(int), typeof(string) };
        var array2 = new[] { typeof(int), typeof(bool) };

        // Act
        var result = comparer.Equals(array1, array2);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TypeArrayComparer_DifferentLengthArrays_ReturnsFalse()
    {
        // Arrange
        var comparer = TypeArrayComparer.Instance;
        var array1 = new[] { typeof(int), typeof(string) };
        var array2 = new[] { typeof(int), typeof(string), typeof(bool) };

        // Act
        var result = comparer.Equals(array1, array2);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TypeArrayComparer_NullArrays_HandledCorrectly()
    {
        // Arrange
        var comparer = TypeArrayComparer.Instance;

        // Act & Assert
        await Assert.That(comparer.Equals(null, null)).IsTrue();
        await Assert.That(comparer.Equals(null, [typeof(int)])).IsFalse();
        await Assert.That(comparer.Equals([typeof(int)], null)).IsFalse();
    }

    [Test]
    public async Task TypeArrayComparer_SameReference_ReturnsTrue()
    {
        // Arrange
        var comparer = TypeArrayComparer.Instance;
        var array = new[] { typeof(int), typeof(string) };

        // Act
        var result = comparer.Equals(array, array);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task TypeArrayComparer_EmptyArrays_ReturnsTrue()
    {
        // Arrange
        var comparer = TypeArrayComparer.Instance;
        var array1 = Array.Empty<Type>();
        var array2 = Array.Empty<Type>();

        // Act
        var result = comparer.Equals(array1, array2);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task TypeArrayComparer_GetHashCode_ConsistentForEqualArrays()
    {
        // Arrange
        var comparer = TypeArrayComparer.Instance;
        var array1 = new[] { typeof(int), typeof(string), typeof(bool) };
        var array2 = new[] { typeof(int), typeof(string), typeof(bool) };

        // Act
        var hash1 = comparer.GetHashCode(array1);
        var hash2 = comparer.GetHashCode(array2);

        // Assert
        await Assert.That(hash1).IsEqualTo(hash2);
    }

    [Test]
    public async Task TypeArrayComparer_GetHashCode_DifferentForDifferentArrays()
    {
        // Arrange
        var comparer = TypeArrayComparer.Instance;
        var array1 = new[] { typeof(int), typeof(string) };
        var array2 = new[] { typeof(bool), typeof(double) };

        // Act
        var hash1 = comparer.GetHashCode(array1);
        var hash2 = comparer.GetHashCode(array2);

        // Assert
        await Assert.That(hash1).IsNotEqualTo(hash2);
    }

    [Test]
    public async Task TypeArrayComparer_ComplexGenericTypes_HandledCorrectly()
    {
        // Arrange
        var comparer = TypeArrayComparer.Instance;
        var array1 = new[] { typeof(List<int>), typeof(Dictionary<string, bool>) };
        var array2 = new[] { typeof(List<int>), typeof(Dictionary<string, bool>) };
        var array3 = new[] { typeof(List<string>), typeof(Dictionary<string, bool>) };

        // Act & Assert
        await Assert.That(comparer.Equals(array1, array2)).IsTrue();
        await Assert.That(comparer.Equals(array1, array3)).IsFalse();
    }
}
