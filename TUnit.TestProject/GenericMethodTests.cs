namespace TUnit.TestProject;

public class GenericMethodTests
{
    [Test]
    [MethodDataSource(nameof(AggregateBy_Numeric_TestData))]
    [MethodDataSource(nameof(AggregateBy_String_TestData))]
    public void AggregateBy_HasExpectedOutput<TSource, TKey, TAccumulate>(
        IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TKey, TAccumulate> seedSelector,
        Func<TAccumulate, TSource, TAccumulate> func,
        IEqualityComparer<TKey>? comparer,
        IEnumerable<KeyValuePair<TKey, TAccumulate>> expected) where TKey : notnull
    {
        var enumerable = source as TSource[] ?? source.ToArray();
        Console.WriteLine(string.Join(", ", enumerable));
        Console.WriteLine(string.Join(", ", enumerable.Select(keySelector)));
        Console.WriteLine(string.Join(", ", enumerable.Select(x => seedSelector(keySelector(x)))));
    }

    public static IEnumerable<(IEnumerable<int> source,
        Func<int, int> keySelector,
        Func<int, int> seedSelector,
        Func<int, int, int> func,
        IEqualityComparer<int>? comparer,
        IEnumerable<KeyValuePair<int, int>> expected)> AggregateBy_Numeric_TestData()
    {
        yield return (
            source: Enumerable.Range(0, 10),
            keySelector: x => x,
            seedSelector: x => 0,
            func: (x, y) => x + y,
            comparer: null,
            expected: Enumerable.Range(0, 10).ToDictionary(x => x, x => x)
        );
    }
    
    public static IEnumerable<(IEnumerable<string> source,
        Func<string, string> keySelector,
        Func<string, string> seedSelector,
        Func<string, string, string> func,
        IEqualityComparer<string>? comparer,
        IEnumerable<KeyValuePair<string, string>> expected)> AggregateBy_String_TestData()
    {
        yield return (
            source: ["Bob", "bob", "tim", "Bob", "Tim"],
            keySelector: x => x,
            seedSelector: x => "",
            func: (x, y) => x + y,
            comparer: null,
            expected: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Bob", "BobBob" },
                { "bob", "bob" },
                { "tim", "tim" },
                { "Tim", "Tim" },
            });
    }
}