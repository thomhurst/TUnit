using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

public interface IIssue6670Test : IIssue6670TestParent
{
    new IList<T> Get<T>() where T : notnull;
}

public interface IIssue6670TestParent
{
    IEnumerable<T> Get<T>() where T : notnull;
}

public class Issue6670Tests
{
    [Test]
    public async Task Hidden_Generic_Interface_Method_Mock_Works()
    {
        var mock = IIssue6670Test.Mock();
        var configured = new List<string> { "configured" };
        mock.Get<string>().Returns(configured);

        IIssue6670Test derived = mock;
        IIssue6670TestParent parent = mock;

        await Assert.That(derived.Get<string>()).IsSameReferenceAs(configured);
        await Assert.That(parent.Get<string>()).IsSameReferenceAs(configured);
    }
}
