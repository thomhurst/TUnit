namespace TUnit.UnitTests;

/// <summary>
/// Tests for TypeArrayComparer functionality
/// </summary>
public class TypeArrayComparerTests
{
    [Test]
    public async Task TypeArrayComparer_EqualArrays_ReturnsTrue()
    {
        var comparer = TypeArrayComparer.Instance;
        var array1 = new[] { typeof(int), typeof(string), typeof(bool) };
        var array2 = new[] { typeof(int), typeof(string), typeof(bool) };
        
        var result = comparer.Equals(array1, array2);
        
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task TypeArrayComparer_DifferentArrays_ReturnsFalse()
    {
        var comparer = TypeArrayComparer.Instance;
        var array1 = new[] { typeof(int), typeof(string) };
        var array2 = new[] { typeof(int), typeof(bool) };
        
        var result = comparer.Equals(array1, array2);
        
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TypeArrayComparer_DifferentLengthArrays_ReturnsFalse()
    {
        var comparer = TypeArrayComparer.Instance;
        var array1 = new[] { typeof(int), typeof(string) };
        var array2 = new[] { typeof(int), typeof(string), typeof(bool) };
        
        var result = comparer.Equals(array1, array2);
        
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TypeArrayComparer_NullArrays_HandledCorrectly()
    {
        var comparer = TypeArrayComparer.Instance;
        
        await Assert.That(comparer.Equals(null, null)).IsTrue();
        await Assert.That(comparer.Equals(null, [typeof(int)])).IsFalse();
        await Assert.That(comparer.Equals([typeof(int)], null)).IsFalse();
    }

    [Test]
    public async Task TypeArrayComparer_SameReference_ReturnsTrue()
    {
        var comparer = TypeArrayComparer.Instance;
        var array = new[] { typeof(int), typeof(string) };
        
        var result = comparer.Equals(array, array);
        
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task TypeArrayComparer_EmptyArrays_ReturnsTrue()
    {
        var comparer = TypeArrayComparer.Instance;
        var array1 = Array.Empty<Type>();
        var array2 = Array.Empty<Type>();
        
        var result = comparer.Equals(array1, array2);
        
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task TypeArrayComparer_GetHashCode_ConsistentForEqualArrays()
    {
        var comparer = TypeArrayComparer.Instance;
        var array1 = new[] { typeof(int), typeof(string), typeof(bool) };
        var array2 = new[] { typeof(int), typeof(string), typeof(bool) };
        
        var hash1 = comparer.GetHashCode(array1);
        var hash2 = comparer.GetHashCode(array2);
        
        await Assert.That(hash1).IsEqualTo(hash2);
    }

    [Test]
    public async Task TypeArrayComparer_GetHashCode_DifferentForDifferentArrays()
    {
        var comparer = TypeArrayComparer.Instance;
        var array1 = new[] { typeof(int), typeof(string) };
        var array2 = new[] { typeof(bool), typeof(double) };
        
        var hash1 = comparer.GetHashCode(array1);
        var hash2 = comparer.GetHashCode(array2);
        
        await Assert.That(hash1).IsNotEqualTo(hash2);
    }

    [Test]
    public async Task TypeArrayComparer_ComplexGenericTypes_HandledCorrectly()
    {
        var comparer = TypeArrayComparer.Instance;
        var array1 = new[] { typeof(List<int>), typeof(Dictionary<string, bool>) };
        var array2 = new[] { typeof(List<int>), typeof(Dictionary<string, bool>) };
        var array3 = new[] { typeof(List<string>), typeof(Dictionary<string, bool>) };
        
        await Assert.That(comparer.Equals(array1, array2)).IsTrue();
        await Assert.That(comparer.Equals(array1, array3)).IsFalse();
    }
}
