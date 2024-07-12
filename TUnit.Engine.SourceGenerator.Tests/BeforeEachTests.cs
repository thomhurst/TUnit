using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Extensions;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class BeforeTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "BeforeTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(5));

            Assert.That(generatedFiles[0].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            TestHookOrchestrator.RegisterSetUp<global::TUnit.TestProject.BeforeTests.Base1>(new InstanceMethod<global::TUnit.TestProject.BeforeTests.Base1>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base1).GetMethod("BeforeEach1", 0, []),
	            Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.BeforeEach1())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[1].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            TestHookOrchestrator.RegisterSetUp<global::TUnit.TestProject.BeforeTests.Base2>(new InstanceMethod<global::TUnit.TestProject.BeforeTests.Base2>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base2).GetMethod("BeforeEach2", 0, []),
	            Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.BeforeEach2())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[2].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            TestHookOrchestrator.RegisterSetUp<global::TUnit.TestProject.BeforeTests.Base3>(new InstanceMethod<global::TUnit.TestProject.BeforeTests.Base3>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base3).GetMethod("BeforeEach3", 0, []),
	            Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.BeforeEach3())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[3].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            TestHookOrchestrator.RegisterSetUp<global::TUnit.TestProject.BeforeTests.SetupTests>(new InstanceMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("Setup", 0, []),
	            Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.Setup())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[4].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            TestHookOrchestrator.RegisterSetUp<global::TUnit.TestProject.BeforeTests.SetupTests>(new InstanceMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
	            { 
	            MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("SetupWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
	            Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.SetupWithContext(testContext))
	            });
	            """.IgnoreWhitespaceFormatting()));
        });
}