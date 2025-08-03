#if !NET
#pragma warning disable CS8767 // Nullability of type parameters in type of collection doesn't match implicitly implemented member (possibly because of nullability attributes).
#endif

using System.Diagnostics.CodeAnalysis;
using System.Text;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Equality;

public class EquivalentToEqualityComparer<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
T>(CompareOptions compareOptions) : IEqualityComparer<T>
{
    public virtual int? EnumerableIndex { get; protected set; }

    public ComparisonFailure[]? ComparisonFailures { get; private set; }

    public EquivalentToEqualityComparer() : this(new CompareOptions())
    {
    }

    public virtual bool Equals(T? x, T? y)
    {
        ComparisonFailures = Compare.CheckEquivalent(x, y, compareOptions, EnumerableIndex).ToArray();

        return ComparisonFailures.Length == 0;
    }

    public int GetHashCode([DisallowNull] T obj)
    {
        return obj.GetHashCode();
    }

    public string GetFailureMessages()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("found the following mismatches:");
        stringBuilder.AppendLine();

        foreach (var comparisonFailure in ComparisonFailures ?? [])
        {
            stringBuilder.AppendLine($"{string.Join(".", comparisonFailure.NestedMemberNames)}:");
            stringBuilder.AppendLine($"\tExpected: {Formatter.Format(comparisonFailure.Expected)}");
            stringBuilder.AppendLine($"\tActual: {Formatter.Format(comparisonFailure.Actual)}");
            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }
}
