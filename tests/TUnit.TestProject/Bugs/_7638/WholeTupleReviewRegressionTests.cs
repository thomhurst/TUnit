using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._7638;

#pragma warning disable TUnit0001 // #7622: whole-tuple rows with omitted tails.
public class MutableWholeTupleReviewTests
{
    public static (Fixture, string) Row() => (new Fixture(), "tuple");
    public static Tuple<Fixture, string> ReferenceRow() => Tuple.Create(new Fixture(), "tuple");

    [Test, MethodDataSource(nameof(Row))]
    public void ValueOptional<T>((T, string) row, int extra = 5) => Verify(row.Item1, row.Item2, extra);

    [Test, MethodDataSource(nameof(ReferenceRow))]
    public void ReferenceOptional<T>(Tuple<T, string> row, int extra = 5) => Verify(row.Item1, row.Item2, extra);

    private static void Verify<T>(T value, string text, int extra)
    {
        if (value is not Fixture { Initialized: true } || text != "tuple" || extra != 5)
            throw new InvalidOperationException("Expected initialized mutable tuple element and optional default");
    }

    public struct Fixture : IAsyncInitializer
    {
        public bool Initialized;
        public Task InitializeAsync()
        {
            Initialized = true;
            return Task.CompletedTask;
        }
    }
}

public class MemberWholeTupleReviewTests
{
    public static IEnumerable<(int, string)> PropertyRows => [(1, "tuple")];
    public static readonly IEnumerable<(int, string)> FieldRows = [(1, "tuple")];

    // The explicit long cases intentionally fail; the inferred int cases must also be discovered.
    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(PropertyRows))]
    public void PropertyOptional<T>((T, string) row, int extra = 5) => Verify(row, extra);

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(FieldRows))]
    public void FieldOptional<T>((T, string) row, int extra = 5) => Verify(row, extra);

    private static void Verify<T>((T, string) row, int extra)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra != 5)
            throw new InvalidOperationException("Expected inferred int whole tuple and optional default");
    }
}
#pragma warning restore TUnit0001
