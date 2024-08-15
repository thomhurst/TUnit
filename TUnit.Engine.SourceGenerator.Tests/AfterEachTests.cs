using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Extensions;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class AfterTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "AfterTests.cs"),
        generatedFiles =>
        {
	        Assert.That(generatedFiles.Length, Is.EqualTo(14));

            Assert.That(generatedFiles[1].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            TestHookOrchestrator.RegisterAfterHook<global::TUnit.TestProject.AfterTests.Base1>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.Base1>
	            {
	                MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base1).GetMethod("AfterEach1", 0, []),
	                Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.AfterEach1()),
	                HookExecutor = DefaultExecutor.Instance,
	                Order = 0,
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[3].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	             TestHookOrchestrator.RegisterAfterHook<global::TUnit.TestProject.AfterTests.Base2>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.Base2>
	             {
	                 MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base2).GetMethod("AfterEach2", 0, []),
	                 Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.AfterEach2()),
	                 HookExecutor = DefaultExecutor.Instance,
	                 Order = 0,
	             });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[5].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	             TestHookOrchestrator.RegisterAfterHook<global::TUnit.TestProject.AfterTests.Base3>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.Base3>
	             {
	                 MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base3).GetMethod("AfterEach3", 0, []),
	               	 Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.AfterEach3()),
	               	 HookExecutor = DefaultExecutor.Instance,
	               	 Order = 0,
	             });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[10].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		             TestHookOrchestrator.RegisterAfterHook<global::TUnit.TestProject.AfterTests.CleanupTests>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
		             {
		               	 MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("Cleanup", 0, []),
		               	 Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.Cleanup()),
		               	 HookExecutor = DefaultExecutor.Instance,
		               	 Order = 0,
		             });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[11].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		             TestHookOrchestrator.RegisterAfterHook<global::TUnit.TestProject.AfterTests.CleanupTests>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
		             {
		               	 MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("Cleanup", 0, [typeof(global::System.Threading.CancellationToken)]),
		               	 Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.Cleanup(cancellationToken)),
		               	 HookExecutor = DefaultExecutor.Instance,
		               	 Order = 0,
		             });
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[12].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		             TestHookOrchestrator.RegisterAfterHook<global::TUnit.TestProject.AfterTests.CleanupTests>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
		             {
		               	 MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("CleanupWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
		               	 Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.CleanupWithContext(testContext)),
		               	 HookExecutor = DefaultExecutor.Instance,
		               	 Order = 0,
		             });
		            """.IgnoreWhitespaceFormatting()));
                        
            Assert.That(generatedFiles[13].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		             TestHookOrchestrator.RegisterAfterHook<global::TUnit.TestProject.AfterTests.CleanupTests>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
		             {
		               	 MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("CleanupWithContext", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
		               	 Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.CleanupWithContext(testContext, cancellationToken)),
		               	 HookExecutor = DefaultExecutor.Instance,
		               	 Order = 0,
		             });
		            """.IgnoreWhitespaceFormatting()));
        });
}