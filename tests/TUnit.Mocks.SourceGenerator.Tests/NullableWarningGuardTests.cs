namespace TUnit.Mocks.SourceGenerator.Tests;

public class NullableWarningGuardTests : SnapshotTestBase
{
    [Test]
    public async Task Rejects_Nullable_Mismatch_In_Generated_Event()
    {
        const string source = """
            #nullable enable
            using System;
            using TUnit.Mocks;

            public interface IService
            {
                event EventHandler<string?>? Changed;
            }

            public class Usage
            {
                void M() => _ = Mock.Of<IService>();
            }
            """;

        AssertGeneratedCodeHasNoNullableWarnings(source);
        var exception = Assert.Throws<InvalidOperationException>(() =>
            AssertGeneratedCodeHasNoNullableWarnings(source, transformGeneratedSource: generated =>
                generated.Replace(
                    "event global::System.EventHandler<string?>? global::IService.Changed",
                    "event global::System.EventHandler<string> global::IService.Changed",
                    StringComparison.Ordinal)));

        await Assert.That(exception.Message).Contains("CS8615");
    }
}
