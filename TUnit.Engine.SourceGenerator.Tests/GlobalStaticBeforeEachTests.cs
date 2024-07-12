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
	            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalBase1).GetMethod("BeforeAll1", 0, []),
	            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalBase1.BeforeAll1())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[1].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            GlobalStaticTestHookOrchestrator.RegisterSetUp(new StaticMethod<TestContext>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalBase2).GetMethod("BeforeAll2", 0, []),
	            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalBase2.BeforeAll2())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[2].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            GlobalStaticTestHookOrchestrator.RegisterSetUp(new StaticMethod<TestContext>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalBase3).GetMethod("BeforeAll3", 0, []),
	            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalBase3.BeforeAll3())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[3].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterSetUp(new StaticMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUp", 0, []),
		            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUp())
		            });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[4].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterSetUp(new StaticMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUp", 0, [typeof(global::System.Threading.CancellationToken)]),
		            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUp(cancellationToken))
		            });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[5].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterSetUp(new StaticMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
		            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUpWithContext(testContext))
		            });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[6].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterSetUp(new StaticMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
		            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUpWithContext(testContext, cancellationToken))
		            });
		            """.IgnoreWhitespaceFormatting()));
        });
}