using System.Numerics;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests for collections containing types that implement IEquatable&lt;T&gt;.
/// Verifies that IsEquivalentTo respects IEquatable implementation rather than using structural comparison.
/// </summary>
public class IEquatableCollectionTests
{
    [Test]
    public async Task Vector2_Array_IsEquivalentTo_List()
    {
        // Arrange - Vector2 implements IEquatable<Vector2>
        var array = new Vector2[]
        {
            new Vector2(1, 2),
            new Vector2(3, 4),
            new Vector2(5, 6),
        };
        var list = new List<Vector2>(array);
        
        // Act & Assert - should use Vector2's IEquatable<Vector2>.Equals
        await Assert.That(array).IsEquivalentTo(list);
    }

    [Test]
    public async Task Uri_Array_IsEquivalentTo_List()
    {
        // Arrange - Uri implements IEquatable<Uri>
        var array = new Uri[]
        {
            new Uri("https://example.com"),
            new Uri("https://github.com"),
            new Uri("https://stackoverflow.com"),
        };
        var list = new List<Uri>(array);
        
        // Act & Assert - should use Uri's IEquatable<Uri>.Equals
        await Assert.That(array).IsEquivalentTo(list);
    }

    [Test]
    public async Task CultureInfo_Array_IsEquivalentTo_List()
    {
        // Arrange - CultureInfo implements IEquatable<CultureInfo>
        var array = new System.Globalization.CultureInfo[]
        {
            new System.Globalization.CultureInfo("en-US"),
            new System.Globalization.CultureInfo("fr-FR"),
            new System.Globalization.CultureInfo("de-DE"),
        };
        var list = new List<System.Globalization.CultureInfo>(array);
        
        // Act & Assert - should use CultureInfo's IEquatable<CultureInfo>.Equals
        await Assert.That(array).IsEquivalentTo(list);
    }

    [Test]
    public async Task Vector3_Array_IsEquivalentTo_List()
    {
        // Arrange - Vector3 implements IEquatable<Vector3>
        var array = new Vector3[]
        {
            new Vector3(1, 2, 3),
            new Vector3(4, 5, 6),
            new Vector3(7, 8, 9),
        };
        var list = new List<Vector3>(array);
        
        // Act & Assert - should use Vector3's IEquatable<Vector3>.Equals
        await Assert.That(array).IsEquivalentTo(list);
    }

    [Test]
    public async Task DateTimeOffset_Array_IsEquivalentTo_List()
    {
        // Arrange - DateTimeOffset implements IEquatable<DateTimeOffset>
        var array = new DateTimeOffset[]
        {
            new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2023, 6, 15, 12, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2023, 12, 31, 23, 59, 59, TimeSpan.Zero),
        };
        var list = new List<DateTimeOffset>(array);
        
        // Act & Assert
        await Assert.That(array).IsEquivalentTo(list);
    }

    [Test]
    public async Task CustomEquatable_Array_IsEquivalentTo_List()
    {
        // Arrange - custom type implementing IEquatable<T>
        var array = new CustomEquatable[]
        {
            new CustomEquatable { Id = 1, Name = "First" },
            new CustomEquatable { Id = 2, Name = "Second" },
            new CustomEquatable { Id = 3, Name = "Third" },
        };
        var list = new List<CustomEquatable>(array);
        
        // Act & Assert - should use CustomEquatable's IEquatable<CustomEquatable>.Equals
        await Assert.That(array).IsEquivalentTo(list);
    }

    [Test]
    public async Task CustomEquatable_Array_NotEquivalent_DifferentValues()
    {
        // Arrange
        var array1 = new CustomEquatable[]
        {
            new CustomEquatable { Id = 1, Name = "First" },
            new CustomEquatable { Id = 2, Name = "Second" },
        };
        var array2 = new CustomEquatable[]
        {
            new CustomEquatable { Id = 1, Name = "First" },
            new CustomEquatable { Id = 3, Name = "Third" }, // Different Id
        };
        
        // Act & Assert
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(array1).IsEquivalentTo(array2);
        });
    }

    /// <summary>
    /// Custom type implementing IEquatable&lt;T&gt; for testing.
    /// </summary>
    public class CustomEquatable : IEquatable<CustomEquatable>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public bool Equals(CustomEquatable? other)
        {
            if (other is null) return false;
            return Id == other.Id && Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            return obj is CustomEquatable other && Equals(other);
        }

        public override int GetHashCode()
        {
#if NET6_0_OR_GREATER
            return HashCode.Combine(Id, Name);
#else
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Id.GetHashCode();
                hash = hash * 23 + (Name?.GetHashCode() ?? 0);
                return hash;
            }
#endif
        }
    }
}
