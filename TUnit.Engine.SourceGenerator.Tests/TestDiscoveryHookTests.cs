using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Extensions;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class TestDiscoveryHookTests : TestsBase<GlobalTestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "TestDiscoveryHookTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles, Has.Length.EqualTo(2));
            
            Assert.That(generatedFiles[0].IgnoreWhitespaceFormatting(), Does.Contain(
                """
                    		GlobalStaticTestHookOrchestrator.RegisterBeforeHook(new StaticHookMethod<BeforeTestDiscoveryContext>
                    		{ 
                               MethodInfo = typeof(global::TUnit.TestProject.TestDiscoveryHookTests).GetMethod("BeforeDiscovery", 0, []),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.TestDiscoveryHookTests.BeforeDiscovery()),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 5,
                               FilePath = @"", 
                               LineNumber = 5,
                    		});
                    """.IgnoreWhitespaceFormatting()
            ));
            
            Assert.That(generatedFiles[1].IgnoreWhitespaceFormatting(), Does.Contain(
                """
                    		GlobalStaticTestHookOrchestrator.RegisterAfterHook(new StaticHookMethod<TestDiscoveryContext>
                    		{ 
                               MethodInfo = typeof(global::TUnit.TestProject.TestDiscoveryHookTests).GetMethod("AfterDiscovery", 0, []),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.TestDiscoveryHookTests.AfterDiscovery()),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"{}", 
                               LineNumber = 10,
                    		});
                    """.IgnoreWhitespaceFormatting()
            ));
        });
}