using System.Collections;

namespace TUnit.TestProject;

[ClassDataSource(typeof(ClassData))]
[Arguments("X")]
[Arguments("Y")]
public class ComprehensiveCountTest(string classValue)
{
    [Test]
    [MethodDataSource(nameof(GetMethodData))]
    [Arguments("A")]
    [Arguments("B")]
    public async Task TestWithMultipleDataSources(string methodValue)
    {
        await Task.CompletedTask;
    }

    public static IEnumerable<string> GetMethodData()
    {
        yield return "M1";
        yield return "M2";
    }

    public class ClassData : IEnumerable<string>
    {
        public IEnumerator<string> GetEnumerator()
        {
            yield return "C1";
            yield return "C2";
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static implicit operator string(ClassData _) => "ClassDataImplicit";
    }
}
