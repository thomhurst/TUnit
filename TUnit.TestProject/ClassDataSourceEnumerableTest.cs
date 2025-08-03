using System.Collections;

namespace TUnit.TestProject;

[ClassDataSource(typeof(EnumerableDataSource))]
public class ClassDataSourceEnumerableTest(string value)
{
    [Test]
    public async Task SimpleTest()
    {
        await Task.CompletedTask;
    }

    public class EnumerableDataSource : IEnumerable<string>
    {
        public IEnumerator<string> GetEnumerator()
        {
            yield return "A";
            yield return "B";
            yield return "C";
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static implicit operator string(EnumerableDataSource source) => "FromImplicitOperator";
    }
}
