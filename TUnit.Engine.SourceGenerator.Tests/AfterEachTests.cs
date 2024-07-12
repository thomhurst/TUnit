using TUnit.Engine.SourceGenerator.CodeGenerators;

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
	        Assert.That(generatedFiles.Length, Is.EqualTo(5));

            Assert.That(generatedFiles[0], Does.Contain(
	            """
	            		TestHookOrchestrator.RegisterCleanUp<global::TUnit.TestProject.AfterTests.Base1>(new InstanceMethod<global::TUnit.TestProject.AfterTests.Base1>
	            		{ 
	               		MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base1).GetMethod("AfterEach1", 0, []),
	               		Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.AfterEach1())
	            		});
	            """));
            
            Assert.That(generatedFiles[1], Does.Contain(
	            """
	            		TestHookOrchestrator.RegisterCleanUp<global::TUnit.TestProject.AfterTests.Base2>(new InstanceMethod<global::TUnit.TestProject.AfterTests.Base2>
	            		{ 
	               		MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base2).GetMethod("AfterEach2", 0, []),
	               		Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.AfterEach2())
	            		});
	            """));
            
            Assert.That(generatedFiles[2], Does.Contain(
	            """
	            		TestHookOrchestrator.RegisterCleanUp<global::TUnit.TestProject.AfterTests.Base3>(new InstanceMethod<global::TUnit.TestProject.AfterTests.Base3>
	            		{ 
	               		MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base3).GetMethod("AfterEach3", 0, []),
	               		Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.AfterEach3())
	            		});
	            """));
            
            Assert.That(generatedFiles[3], Does.Contain(
	            """
	            		TestHookOrchestrator.RegisterCleanUp<global::TUnit.TestProject.AfterTests.CleanupTests>(new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
	            		{ 
	               		MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("Cleanup", 0, []),
	               		Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.Cleanup())
	            		});
	            """));
            
            Assert.That(generatedFiles[4], Does.Contain(
	            """
	            		TestHookOrchestrator.RegisterCleanUp<global::TUnit.TestProject.AfterTests.CleanupTests>(new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
	            		{ 
	               		MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("CleanupWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
	               		Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.CleanupWithContext(testContext))
	            		});
	            """));
        });
}