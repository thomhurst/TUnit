using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class BeforeTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "BeforeTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(14);

            await AssertFileContains(generatedFiles[1], 
                """
                new InstanceHookMethod<global::TUnit.TestProject.BeforeTests.Base1>
                { 
                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base1).GetMethod("BeforeEach1", 0, []),
                Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.BeforeEach1()),
                HookExecutor = DefaultExecutor.Instance,
                Order = 0,
                },
                """);
            
            await AssertFileContains(generatedFiles[3], 
                """
                new InstanceHookMethod<global::TUnit.TestProject.BeforeTests.Base2>
                { 
                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base2).GetMethod("BeforeEach2", 0, []),
                Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.BeforeEach2()),
                HookExecutor = DefaultExecutor.Instance,
                Order = 0,
                },
                """);
            
            await AssertFileContains(generatedFiles[5], 
                """
                new InstanceHookMethod<global::TUnit.TestProject.BeforeTests.Base3>
                { 
                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base3).GetMethod("BeforeEach3", 0, []),
                Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.BeforeEach3()),
                HookExecutor = DefaultExecutor.Instance,
                Order = 0,
                },
                """);
            
            await AssertFileContains(generatedFiles[10], 
                """
                    new InstanceHookMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("Setup", 0, []),
                    Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.Setup()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    },
                    """);
            
            await AssertFileContains(generatedFiles[11], 
                """
                    new InstanceHookMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("Setup", 0, [typeof(global::System.Threading.CancellationToken)]),
                    Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.Setup(cancellationToken)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    },
                    """);
            
            await AssertFileContains(generatedFiles[12], 
                """
                    new InstanceHookMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("SetupWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
                    Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.SetupWithContext(context)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    },
                    """);
            
            await AssertFileContains(generatedFiles[13], 
                """
                    new InstanceHookMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("SetupWithContext", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
                    Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.SetupWithContext(context, cancellationToken)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    },
                    """);
        });
}