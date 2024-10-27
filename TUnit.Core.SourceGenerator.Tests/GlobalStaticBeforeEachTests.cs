using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class GlobalStaticBeforeEachTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "BeforeEveryTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(14);
            
            await AssertFileContains(generatedFiles[7], 
                """
                new StaticHookMethod<global::TUnit.Core.TestContext>
                { 
                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalBase1).GetMethod("BeforeAll1", 0, [typeof(global::TUnit.Core.TestContext)]),
                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalBase1.BeforeAll1(context)),
                HookExecutor = DefaultExecutor.Instance,
                Order = 0,
                FilePath = @"", 
                LineNumber = 5,
                },
                """);
            
            await AssertFileContains(generatedFiles[8], 
                """
                new StaticHookMethod<global::TUnit.Core.TestContext>
                { 
                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalBase2).GetMethod("BeforeAll2", 0, [typeof(global::TUnit.Core.TestContext)]),
                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalBase2.BeforeAll2(context)),
                HookExecutor = DefaultExecutor.Instance,
                Order = 0,
                FilePath = @"", 
                LineNumber = 20,
                },
                """);
            
            await AssertFileContains(generatedFiles[9], 
                """
                new StaticHookMethod<global::TUnit.Core.TestContext>
                { 
                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalBase3).GetMethod("BeforeAll3", 0, [typeof(global::TUnit.Core.TestContext)]),
                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalBase3.BeforeAll3(context)),
                HookExecutor = DefaultExecutor.Instance,
                Order = 0,
                FilePath = @"", 
                LineNumber = 35,
                },
                """);
            
            await AssertFileContains(generatedFiles[10], 
                """
                    new StaticHookMethod<global::TUnit.Core.TestContext>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUp", 0, [typeof(global::TUnit.Core.TestContext)]),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUp(context)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 50,
                    },
                    """);
            
            await AssertFileContains(generatedFiles[11], 
                """
                    new StaticHookMethod<global::TUnit.Core.TestContext>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUp", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUp(context, cancellationToken)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 56,
                    },
                    """);
            
            await AssertFileContains(generatedFiles[12], 
                """
                    new StaticHookMethod<global::TUnit.Core.TestContext>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUpWithContext(context)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 62,
                    },
                    """);
            
            await AssertFileContains(generatedFiles[13], 
                """
                    new StaticHookMethod<global::TUnit.Core.TestContext>
                    { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.GlobalSetUpTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.TestContext), typeof(global::System.Threading.CancellationToken)]),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.GlobalSetUpTests.BeforeAllSetUpWithContext(context, cancellationToken)),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 68,
                    },
                    """);
        });
}