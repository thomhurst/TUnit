using TUnit.Mocks;
using TUnit.Mocks.Generated;

namespace TUnit.TestProject.Bugs._6670;

public interface ITest : ITestParent
{
    new IList<T> Get<T>() where T : notnull;
}

public interface ITestParent
{
    IEnumerable<T> Get<T>() where T : notnull;
}

public class Issue6670MockTests
{
    private readonly ITestMock _test = ITest.Mock();

    [Test]
    public async Task Hidden_Generic_Interface_Method_Mock_Works()
    {
        var configured = new List<string> { "configured" };
        _test.Get<string>().Returns(configured);

        ITest derived = _test;
        ITestParent parent = _test;

        await Assert.That(derived.Get<string>()).IsSameReferenceAs(configured);
        await Assert.That(parent.Get<string>()).IsSameReferenceAs(configured);
    }
}
