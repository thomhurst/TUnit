using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class AfterTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "AfterTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(14);

            await AssertFileContains(generatedFiles[1], 
                """
                TestRegistrar.RegisterAfterHook<global::TUnit.TestProject.AfterTests.Base1>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.Base1>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base1).GetMethod("AfterEach1", 0, []),
                    Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.AfterEach1()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                });
                """);
            
            await AssertFileContains(generatedFiles[3], 
                """
                 TestRegistrar.RegisterAfterHook<global::TUnit.TestProject.AfterTests.Base2>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.Base2>
                 {
                     MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base2).GetMethod("AfterEach2", 0, []),
                     Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.AfterEach2()),
                     HookExecutor = DefaultExecutor.Instance,
                     Order = 0,
                 });
                """);
            
            await AssertFileContains(generatedFiles[5], 
                """
                 TestRegistrar.RegisterAfterHook<global::TUnit.TestProject.AfterTests.Base3>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.Base3>
                 {
                     MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base3).GetMethod("AfterEach3", 0, []),
                        Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.AfterEach3()),
                        HookExecutor = DefaultExecutor.Instance,
                        Order = 0,
                 });
                """);
            
            await AssertFileContains(generatedFiles[10], 
                """
                     TestRegistrar.RegisterAfterHook<global::TUnit.TestProject.AfterTests.CleanupTests>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                     {
                            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("Cleanup", 0, []),
                            Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.Cleanup()),
                            HookExecutor = DefaultExecutor.Instance,
                            Order = 0,
                     });
                    """);
            
            await AssertFileContains(generatedFiles[11], 
                """
                     TestRegistrar.RegisterAfterHook<global::TUnit.TestProject.AfterTests.CleanupTests>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                     {
                            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("Cleanup", 0, [typeof(global::System.Threading.CancellationToken)]),
                            Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.Cleanup(cancellationToken)),
                            HookExecutor = DefaultExecutor.Instance,
                            Order = 0,
                     });
                    """);
            
            await AssertFileContains(generatedFiles[12], 
                """
                     TestRegistrar.RegisterAfterHook<global::TUnit.TestProject.AfterTests.CleanupTests>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                     {
                            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("CleanupWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
                            Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.CleanupWithContext(testContext)),
                            HookExecutor = DefaultExecutor.Instance,
                            Order = 0,
                     });
                    """);
                        
            await AssertFileContains(generatedFiles[13], 
                """
                     TestRegistrar.RegisterAfterHook<global::TUnit.TestProject.AfterTests.CleanupTests>(new InstanceHookMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                     {
                            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("CleanupWithContext", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
                            Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.CleanupWithContext(testContext, cancellationToken)),
                            HookExecutor = DefaultExecutor.Instance,
                            Order = 0,
                     });
                    """);
        });
}