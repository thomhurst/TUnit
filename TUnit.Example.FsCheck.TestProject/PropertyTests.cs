using FsCheck;
using FsCheck.Fluent;
using TUnit.Core;
using TUnit.FsCheck;

namespace TUnit.Example.FsCheck.TestProject;

public class PropertyTests
{
    [Test, FsCheckProperty]
    public bool ReverseReverseIsOriginal(int[] array)
    {
        var reversed = array.AsEnumerable().Reverse().Reverse().ToArray();
        return array.SequenceEqual(reversed);
    }

    [Test, FsCheckProperty]
    public bool AbsoluteValueIsNonNegative(int value)
    {
        return Math.Abs((long)value) >= 0;
    }

    [Test, FsCheckProperty]
    public bool StringConcatenationLength(string a, string b)
    {
        if (a == null || b == null)
        {
            return true; // Skip null cases
        }

        return (a + b).Length == a.Length + b.Length;
    }

    [Test, FsCheckProperty(MaxTest = 50)]
    public bool ListConcatenationPreservesElements(int[] first, int[] second)
    {
        var combined = first.Concat(second).ToArray();
        return combined.Length == first.Length + second.Length;
    }

    [Test, FsCheckProperty]
    public void AdditionIsCommutative(int a, int b)
    {
        var result1 = a + b;
        var result2 = b + a;

        if (result1 != result2)
        {
            throw new InvalidOperationException($"Addition is not commutative: {a} + {b} = {result1}, {b} + {a} = {result2}");
        }
    }

    [Test, FsCheckProperty]
    public async Task AsyncPropertyTest(int value)
    {
        await Task.Delay(1); // Simulate async work

        if (value * 0 != 0)
        {
            throw new InvalidOperationException("Multiplication by zero should always be zero");
        }
    }

    [Test, FsCheckProperty]
    public bool MultiplicationIsAssociative(int a, int b, int c)
    {
        // Using long to avoid overflow
        var left = (long)a * ((long)b * c);
        var right = ((long)a * b) * c;
        return left == right;
    }

    [Test, FsCheckProperty]
    public bool SumOfFourNumbersIsCommutative(int a, int b, int c, int d)
    {
        var sum1 = a + b + c + d;
        var sum2 = d + c + b + a;
        return sum1 == sum2;
    }

    [Test, FsCheckProperty]
    public Property StringReversalProperty()
    {
        return Prop.ForAll<string>(str =>
        {
            var reversed = new string(str.Reverse().ToArray());
            var doubleReversed = new string(reversed.Reverse().ToArray());
            return str == doubleReversed;
        });
    }
}
