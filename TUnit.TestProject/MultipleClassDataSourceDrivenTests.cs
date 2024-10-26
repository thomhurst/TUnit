#pragma warning disable CS9113 // Parameter is unread.

namespace TUnit.TestProject;

[ClassDataSource<Inject1, Inject2, Inject3, Inject4, Inject5>(Shared = [SharedType.None, SharedType.None, SharedType.None, SharedType.None, SharedType.None])]
public class MultipleClassDataSourceDrivenTests(
    MultipleClassDataSourceDrivenTests.Inject1 inject1, 
    MultipleClassDataSourceDrivenTests.Inject2 inject2, 
    MultipleClassDataSourceDrivenTests.Inject3 inject3, 
    MultipleClassDataSourceDrivenTests.Inject4 inject4, 
    MultipleClassDataSourceDrivenTests.Inject5 inject5
    )
{
    [Test]
    public void Test1()
    {
        // Dummy method
    }

    [Test]
    public void Test2()
    {
        // Dummy method
    }

    public class Inject1;
    public class Inject2;
    public class Inject3;
    public class Inject4;
    public class Inject5;
}