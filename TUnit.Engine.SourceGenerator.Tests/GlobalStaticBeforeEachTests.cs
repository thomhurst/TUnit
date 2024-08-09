using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Extensions;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class GlobalStaticBeforeEachTests : TestsBase<GlobalTestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "GlobalBeforeTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(7));

            Assert.That(generatedFiles[0].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            GlobalStaticTestHookOrchestrator.RegisterSetUp(new StaticMethod<TestContext>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalBase1).GetMethod("BeforeAll1", 0, [typeof(global::TUnit.Core.TestContext)]),
	            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalBase1.BeforeAll1(context)),
	            HookExecutor = DefaultExecutor.Instance,
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[1].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            GlobalStaticTestHookOrchestrator.RegisterSetUp(new StaticMethod<TestContext>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalBase2).GetMethod("BeforeAll2", 0, [typeof(global::TUnit.Core.TestContext)]),
	            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalBase2.BeforeAll2(context)),
	            HookExecutor = DefaultExecutor.Instance,
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[2].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            GlobalStaticTestHookOrchestrator.RegisterSetUp(new StaticMethod<TestContext>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalBase3).GetMethod("BeforeAll3", 0, [typeof(global::TUnit.Core.TestContext)]),
	            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalBase3.BeforeAll3(context)),
	            HookExecutor = DefaultExecutor.Instance,
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[3].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterSetUp(new StaticMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUp", 0, [typeof(global::TUnit.Core.TestContext)]),
		            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUp(context)),
		            HookExecutor = DefaultExecutor.Instance,
		            });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[4].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterSetUp(new StaticMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUp", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
		            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUp(context, cancellationToken)),
		            HookExecutor = DefaultExecutor.Instance,
		            });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[5].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterSetUp(new StaticMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
		            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUpWithContext(context)),
		            HookExecutor = DefaultExecutor.Instance,
		            });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[6].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterSetUp(new StaticMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
		            Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUpWithContext(context, cancellationToken)),
		            HookExecutor = DefaultExecutor.Instance,
		            });
		            """.IgnoreWhitespaceFormatting()));
        });
}