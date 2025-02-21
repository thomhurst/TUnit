using TUnit.Core;

namespace TUnit.TestProject.Library.Bugs._1889;

public abstract class BaseTest<T>
{
    [Test]
    public void Test1()
    {
    }

    [Test]
    [MatrixDataSource]
    public void Test2(bool condition)
    {
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public void Test3(bool condition)
    {
    }
}