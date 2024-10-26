using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

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
                new InstanceHookMethod<global::TUnit.TestProject.AfterTests.Base1>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base1).GetMethod("AfterEach1", 0, []),
                    Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.AfterEach1()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                },
                """);
            
            await AssertFileContains(generatedFiles[3], 
                """
                 new InstanceHookMethod<global::TUnit.TestProject.AfterTests.Base2>
                 {
                     MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base2).GetMethod("AfterEach2", 0, []),
                     Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.AfterEach2()),
                     HookExecutor = DefaultExecutor.Instance,
                     Order = 0,
                 },
                """);
            
            await AssertFileContains(generatedFiles[5], 
                """
                 new InstanceHookMethod<global::TUnit.TestProject.AfterTests.Base3>
                 {
                     MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base3).GetMethod("AfterEach3", 0, []),
                        Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.AfterEach3()),
                        HookExecutor = DefaultExecutor.Instance,
                        Order = 0,
                 },
                """);
            
            await AssertFileContains(generatedFiles[10], 
                """
                     new InstanceHookMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                     {
                            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("Cleanup", 0, []),
                            Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.Cleanup()),
                            HookExecutor = DefaultExecutor.Instance,
                            Order = 0,
                     },
                    """);
            
            await AssertFileContains(generatedFiles[11], 
                """
                     new InstanceHookMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                     {
                            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("Cleanup", 0, [typeof(global::System.Threading.CancellationToken)]),
                            Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.Cleanup(cancellationToken)),
                            HookExecutor = DefaultExecutor.Instance,
                            Order = 0,
                     },
                    """);
            
            await AssertFileContains(generatedFiles[12], 
                """
                     new InstanceHookMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                     {
                            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("CleanupWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
                            Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.CleanupWithContext(context)),
                            HookExecutor = DefaultExecutor.Instance,
                            Order = 0,
                     },
                    """);
                        
            await AssertFileContains(generatedFiles[13], 
                """
                     new InstanceHookMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                     {
                            MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("CleanupWithContext", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
                            Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.CleanupWithContext(context, cancellationToken)),
                            HookExecutor = DefaultExecutor.Instance,
                            Order = 0,
                     },
                    """);
        });
}