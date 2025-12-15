using System.Numerics;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests to ensure the workaround mentioned in the issue still works.
/// Users should be able to explicitly pass EqualityComparer&lt;T&gt;.Default if needed.
/// </summary>
public class IEquatableWorkaroundTests
{
    [Test]
    public async Task Vector2_Array_IsEquivalentTo_WithExplicitComparer()
    {
        // Arrange
        var array = new Vector2[]
        {
            new Vector2(1, 2),
            new Vector2(3, 4),
            new Vector2(5, 6),
        };
        var list = new List<Vector2>(array);
        
        // Act & Assert - explicitly passing EqualityComparer<Vector2>.Default
        await Assert.That(array).IsEquivalentTo(list, EqualityComparer<Vector2>.Default);
    }

    [Test]
    public async Task CustomEquatable_Array_IsEquivalentTo_WithCustomComparer()
    {
        // Arrange
        var array = new CustomEquatable[]
        {
            new CustomEquatable { Id = 1, Name = "First" },
            new CustomEquatable { Id = 2, Name = "Second" },
        };
        var list = new List<CustomEquatable>(array);
        
        // Act & Assert - using a custom comparer that only compares Id
        await Assert.That(array).IsEquivalentTo(list, new IdOnlyComparer());
    }

    [Test]
    public async Task CustomEquatable_Array_WithDifferentNames_EquivalentWithCustomComparer()
    {
        // Arrange
        var array = new CustomEquatable[]
        {
            new CustomEquatable { Id = 1, Name = "First" },
            new CustomEquatable { Id = 2, Name = "Second" },
        };
        var list = new List<CustomEquatable>
        {
            new CustomEquatable { Id = 1, Name = "Different" },
            new CustomEquatable { Id = 2, Name = "Names" },
        };
        
        // Act & Assert - custom comparer ignores Name
        await Assert.That(array).IsEquivalentTo(list, new IdOnlyComparer());
    }

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

    private class IdOnlyComparer : IEqualityComparer<CustomEquatable>
    {
        public bool Equals(CustomEquatable? x, CustomEquatable? y)
        {
            if (x is null && y is null) return true;
            if (x is null || y is null) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(CustomEquatable obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
