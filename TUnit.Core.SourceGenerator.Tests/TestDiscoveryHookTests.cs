using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

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
                            new StaticHookMethod<global::TUnit.Core.BeforeTestDiscoveryContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.TestDiscoveryHookTests).GetMethod("BeforeDiscovery", 0, []),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.TestDiscoveryHookTests.BeforeDiscovery()),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 5,
                               FilePath = @"", 
                               LineNumber = 5,
                            },
                    """);
            
            await AssertFileContains(generatedFiles[1], 
                """
                            new StaticHookMethod<global::TUnit.Core.TestDiscoveryContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.TestDiscoveryHookTests).GetMethod("AfterDiscovery", 0, []),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.TestDiscoveryHookTests.AfterDiscovery()),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 10,
                            },
                    """);
        });
}