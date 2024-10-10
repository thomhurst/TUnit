using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class GlobalStaticAfterEachTests : TestsBase<GlobalTestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "AfterEveryTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(7);

            await AssertFileContains(generatedFiles[0], 
                """
                TestRegistrar.RegisterAfterHook(new StaticHookMethod<TestContext>
                { 
                MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalBase1).GetMethod("AfterAll1", 0, [typeof(global::TUnit.Core.TestContext)]),
                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalBase1.AfterAll1(context)),
                HookExecutor = DefaultExecutor.Instance,
                Order = 0,
                FilePath = @"{}", 
                LineNumber = 5,
                });
                """);
            
            await AssertFileContains(generatedFiles[1], 
                """
                TestRegistrar.RegisterAfterHook(new StaticHookMethod<TestContext>
                { 
                MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalBase2).GetMethod("AfterAll2", 0, [typeof(global::TUnit.Core.TestContext)]),
                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalBase2.AfterAll2(context)),
                HookExecutor = DefaultExecutor.Instance,
                Order = 0,
                FilePath = @"{}", 
                LineNumber = 20,
                });
                """);
            
            await AssertFileContains(generatedFiles[2], 
                """
                TestRegistrar.RegisterAfterHook(new StaticHookMethod<TestContext>
                { 
                MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalBase3).GetMethod("AfterAll3", 0, [typeof(global::TUnit.Core.TestContext)]),
                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalBase3.AfterAll3(context)),
                HookExecutor = DefaultExecutor.Instance,
                Order = 0,
                FilePath = @"{}", 
                LineNumber = 35,
                });
                """);
            
            await AssertFileContains(generatedFiles[3], 
                """
                    TestRegistrar.RegisterAfterHook(new StaticHookMethod<TestContext>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalCleanUpTests).GetMethod("AfterAllCleanUp", 0, [typeof(global::TUnit.Core.TestContext)]),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalCleanUpTests.AfterAllCleanUp(context)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"{}", 
                    LineNumber = 50,
                    });
                    """);
            
            await AssertFileContains(generatedFiles[4], 
                """
                    TestRegistrar.RegisterAfterHook(new StaticHookMethod<TestContext>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalCleanUpTests).GetMethod("AfterAllCleanUp", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalCleanUpTests.AfterAllCleanUp(context, cancellationToken)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"{}", 
                    LineNumber = 56,
                    });
                    """);
            
            await AssertFileContains(generatedFiles[5], 
                """
                    TestRegistrar.RegisterAfterHook(new StaticHookMethod<TestContext>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalCleanUpTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalCleanUpTests.AfterAllCleanUpWithContext(context)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"{}", 
                    LineNumber = 62,
                    });
                    """);
            
            await AssertFileContains(generatedFiles[6], 
                """
                    TestRegistrar.RegisterAfterHook(new StaticHookMethod<TestContext>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.GlobalCleanUpTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.GlobalCleanUpTests.AfterAllCleanUpWithContext(context, cancellationToken)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"{}", 
                    LineNumber = 68,
                    });
                    """);
        });
}