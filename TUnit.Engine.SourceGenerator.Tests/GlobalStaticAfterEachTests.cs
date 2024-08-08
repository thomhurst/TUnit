using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Extensions;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class GlobalStaticAfterEachTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "GlobalAfterTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(14));

            Assert.That(generatedFiles[0].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticMethod<TestContext>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalBase1).GetMethod("AfterAll1", 0, []),
	            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalBase1.AfterAll1())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[2].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticMethod<TestContext>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalBase2).GetMethod("AfterAll2", 0, []),
	            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalBase2.AfterAll2())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[4].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticMethod<TestContext>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalBase3).GetMethod("AfterAll3", 0, []),
	            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalBase3.AfterAll3())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[6].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalCleanUpTests).GetMethod("AfterAllCleanUp", 0, []),
		            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalCleanUpTests.AfterAllCleanUp())
		            });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[7].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalCleanUpTests).GetMethod("AfterAllCleanUp", 0, [typeof(global::System.Threading.CancellationToken)]),
		            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalCleanUpTests.AfterAllCleanUp(cancellationToken))
		            });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[8].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalCleanUpTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
		            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalCleanUpTests.AfterAllCleanUpWithContext(testContext))
		            });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[9].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            GlobalStaticTestHookOrchestrator.RegisterCleanUp(new StaticMethod<TestContext>
		            { 
		            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalCleanUpTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
		            Body = (testContext, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalCleanUpTests.AfterAllCleanUpWithContext(testContext, cancellationToken))
		            });
		            """.IgnoreWhitespaceFormatting()));
        });
}