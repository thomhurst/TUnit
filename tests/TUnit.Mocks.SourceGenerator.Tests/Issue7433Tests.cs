namespace TUnit.Mocks.SourceGenerator.Tests;

public class Issue7433Tests : SnapshotTestBase
{
    [Test]
    public async Task Predicate_Locals_Avoid_Parameter_Names()
    {
        const string source = """
            using TUnit.Mocks;

            public interface IService
            {
                void Send(string value, string __fa_value, string __fa_value_1);
                void Escaped(string @event, string @__fa_event);
            }

            public class Usage
            {
                void M() => _ = Mock.Of<IService>();
            }
            """;

        await VerifyGeneratorOutput(source);
    }
}
