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
        await Assert.That(new[] { "X", "Y", "C1", "C2", "ClassDataImplicit" }).Contains(classValue);
        await Assert.That(new[] { "A", "B", "M1", "M2" }).Contains(methodValue);
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
