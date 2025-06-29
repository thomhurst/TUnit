using TUnit.Core;

namespace TUnit.TestProject;

[MethodDataSource(nameof(ClassData))]
public class ClassLevelDataSourceTest
{
    public ClassLevelDataSourceTest(int value)
    {
        Console.WriteLine($"Constructor called with value: {value}");
    }

    [Test]
    public void SimpleTest()
    {
        Console.WriteLine("Test executed");
    }

    public static int[] ClassData() => new[] { 1, 2, 3 };
}
