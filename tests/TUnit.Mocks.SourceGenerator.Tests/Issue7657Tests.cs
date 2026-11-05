namespace TUnit.Mocks.SourceGenerator.Tests;

public class Issue7657Tests : SnapshotTestBase
{
    [Test]
    public Task Setup_Identifiers_Snapshot()
        => VerifyGeneratorOutput("""
            using TUnit.Mocks;
            namespace TestNamespace;
            public interface IService { void Send(string mock, string matchers); }
            public class Usage { void M() => _ = Mock.Of<IService>(); }
            """);
}
