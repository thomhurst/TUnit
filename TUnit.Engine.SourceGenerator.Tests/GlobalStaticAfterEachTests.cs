using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Extensions;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class GlobalStaticAfterEachTests : TestsBase<GlobalTestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "GlobalAfterTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(7));

            Assert.That(generatedFiles[0].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticHookMethod<TestContext>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalBase1).GetMethod("AfterAll1", 0, [typeof(global::TUnit.Core.TestContext)]),
	            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalBase1.AfterAll1(context)),
	            HookExecutor = DefaultExecutor.Instance,
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[1].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticHookMethod<TestContext>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalBase2).GetMethod("AfterAll2", 0, [typeof(global::TUnit.Core.TestContext)]),
	            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalBase2.AfterAll2(context)),
	            HookExecutor = DefaultExecutor.Instance,
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[2].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticHookMethod<TestContext>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalBase3).GetMethod("AfterAll3", 0, [typeof(global::TUnit.Core.TestContext)]),
	            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalBase3.AfterAll3(context)),
	            HookExecutor = DefaultExecutor.Instance,
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[3].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticHookMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalCleanUpTests).GetMethod("AfterAllCleanUp", 0, [typeof(global::TUnit.Core.TestContext)]),
		            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalCleanUpTests.AfterAllCleanUp(context)),
		            HookExecutor = DefaultExecutor.Instance,
		            });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[4].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticHookMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalCleanUpTests).GetMethod("AfterAllCleanUp", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
		            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalCleanUpTests.AfterAllCleanUp(context, cancellationToken)),
		            HookExecutor = DefaultExecutor.Instance,
		            });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[5].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticHookMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalCleanUpTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
		            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalCleanUpTests.AfterAllCleanUpWithContext(context)),
		            HookExecutor = DefaultExecutor.Instance,
		            });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[6].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticHookMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalCleanUpTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
		            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalCleanUpTests.AfterAllCleanUpWithContext(context, cancellationToken)),
		            HookExecutor = DefaultExecutor.Instance,
		            });
		            """.IgnoreWhitespaceFormatting()));
        });
}