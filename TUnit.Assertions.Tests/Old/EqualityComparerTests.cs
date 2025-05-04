#pragma warning disable

using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class EqualityComparerTests
{
    public class Comparer : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => true;

        public int GetHashCode(double obj) => obj.GetHashCode();
    }


    [Test]
    public async Task ComparerTestFailureAsync()
    {
        const double a = 10;
        const double b = 0;
        await TUnitAssert.That(a).IsEqualTo(b, new Comparer());
    }

    [Test]
    public async Task ComparerTestSuccessAsync()
    {
        const double a = 10;
        const double b = 0;
        await TUnitAssert.That(new Comparer().Equals(a, b)).IsTrue();
    }
}