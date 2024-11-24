using TUnit.Assertions.Assertions.Collections;

namespace TUnit.Core.SourceGenerator.Tests;

internal class TestDiscoveryHookTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "TestDiscoveryHookTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(2);
            
            await AssertFileContains(generatedFiles[0], 
                """
                            new global::TUnit.Core.Hooks.BeforeTestDiscoveryHookMethod
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.TestDiscoveryHookTests).GetMethod("BeforeDiscovery", 0, []),
                               Body = (context, cancellationToken) => global::TUnit.TestProject.TestDiscoveryHookTests.BeforeDiscovery(),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 5,
                               FilePath = @"", 
                               LineNumber = 5,
                            },
                    """);
            
            await AssertFileContains(generatedFiles[1], 
                """
                            new global::TUnit.Core.Hooks.AfterTestDiscoveryHookMethod
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.TestDiscoveryHookTests).GetMethod("AfterDiscovery", 0, []),
                               Body = (context, cancellationToken) => global::TUnit.TestProject.TestDiscoveryHookTests.AfterDiscovery(),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 10,
                            },
                    """);
        });
}