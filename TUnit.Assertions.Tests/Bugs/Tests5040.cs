namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Tests for issue #5040: IsEquivalentTo throws NotSupportedException on IEquatable types with private state.
/// When a type has no public members but implements IEquatable, structural comparison should fall back to Equals().
/// </summary>
public class Tests5040
{
    #region Test Types

    private sealed class PrivateStatePath : IEquatable<PrivateStatePath>
    {
        private readonly string _value;

        public PrivateStatePath(string value)
        {
            _value = value;
        }

        public bool Equals(PrivateStatePath? other)
        {
            if (other is null)
            {
                return false;
            }

            return _value == other._value;
        }

        public override bool Equals(object? obj) => Equals(obj as PrivateStatePath);

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => _value;
    }

    #endregion

    [Test]
    public async Task IEquatable_Class_With_No_Public_Members_Equal_Values_Passes()
    {
        var path1 = new PrivateStatePath("/home/user/file.txt");
        var path2 = new PrivateStatePath("/home/user/file.txt");

        await Assert.That(path1).IsEquivalentTo(path2);
    }

    [Test]
    public async Task IEquatable_Class_With_No_Public_Members_Different_Values_Fails()
    {
        var path1 = new PrivateStatePath("/home/user/file1.txt");
        var path2 = new PrivateStatePath("/home/user/file2.txt");

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(path1).IsEquivalentTo(path2));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task IEquatable_Class_Collection_Unordered_Passes()
    {
        // Reproduces the exact scenario from issue #5040
        var list = new List<PrivateStatePath>
        {
            new("/home/user/a.txt"),
            new("/home/user/b.txt"),
            new("/home/user/c.txt"),
        };

        var expected = new List<PrivateStatePath>
        {
            new("/home/user/c.txt"),
            new("/home/user/a.txt"),
            new("/home/user/b.txt"),
        };

        await Assert.That(list).IsEquivalentTo(expected);
    }
}
