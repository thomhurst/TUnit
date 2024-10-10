using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class GlobalStaticBeforeEachTests : TestsBase<GlobalTestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "BeforeEveryTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(7);

            await AssertFileContains(generatedFiles[0], 
                """
                TestRegistrar.RegisterBeforeHook(new StaticHookMethod<TestContext>
                { 
                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalBase1).GetMethod("BeforeAll1", 0, [typeof(global::TUnit.Core.TestContext)]),
                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalBase1.BeforeAll1(context)),
                HookExecutor = DefaultExecutor.Instance,
                Order = 0,
                FilePath = @"", 
                LineNumber = 5,
                });
                """);
            
            await AssertFileContains(generatedFiles[1], 
                """
                TestRegistrar.RegisterBeforeHook(new StaticHookMethod<TestContext>
                { 
                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalBase2).GetMethod("BeforeAll2", 0, [typeof(global::TUnit.Core.TestContext)]),
                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalBase2.BeforeAll2(context)),
                HookExecutor = DefaultExecutor.Instance,
                Order = 0,
                FilePath = @"", 
                LineNumber = 20,
                });
                """);
            
            await AssertFileContains(generatedFiles[2], 
                """
                TestRegistrar.RegisterBeforeHook(new StaticHookMethod<TestContext>
                { 
                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalBase3).GetMethod("BeforeAll3", 0, [typeof(global::TUnit.Core.TestContext)]),
                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalBase3.BeforeAll3(context)),
                HookExecutor = DefaultExecutor.Instance,
                Order = 0,
                FilePath = @"", 
                LineNumber = 35,
                });
                """);
            
            await AssertFileContains(generatedFiles[3], 
                """
                    TestRegistrar.RegisterBeforeHook(new StaticHookMethod<TestContext>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUp", 0, [typeof(global::TUnit.Core.TestContext)]),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUp(context)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 50,
                    });
                    """);
            
            await AssertFileContains(generatedFiles[4], 
                """
                    TestRegistrar.RegisterBeforeHook(new StaticHookMethod<TestContext>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUp", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUp(context, cancellationToken)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 56,
                    });
                    """);
            
            await AssertFileContains(generatedFiles[5], 
                """
                    TestRegistrar.RegisterBeforeHook(new StaticHookMethod<TestContext>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUpWithContext(context)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 62,
                    });
                    """);
            
            await AssertFileContains(generatedFiles[6], 
                """
                    TestRegistrar.RegisterBeforeHook(new StaticHookMethod<TestContext>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUpWithContext(context, cancellationToken)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 68,
                    });
                    """);
        });
}