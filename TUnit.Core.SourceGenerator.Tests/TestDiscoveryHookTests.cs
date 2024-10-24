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
            await AssertFileContains(generatedFiles[0],
                """
                        SourceRegistrar.Register(new TestDiscoveryHooks_TUnit_TestProject_TestDiscoveryHookTests());
                """);

            await AssertFileContains(generatedFiles[0],
                """
                new StaticHookMethod<BeforeTestDiscoveryContext>
                { 
                   MethodInfo = typeof(global::TUnit.TestProject.TestDiscoveryHookTests).GetMethod("BeforeDiscovery", 0, []),
                   Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.TestDiscoveryHookTests.BeforeDiscovery()),
                   HookExecutor = DefaultExecutor.Instance,
                   Order = 5,
                   FilePath = @"",
                   LineNumber = 5,
                },
                """
            );
            
            await AssertFileContains(generatedFiles[0], 
                """
                    new StaticHookMethod<TestDiscoveryContext>
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