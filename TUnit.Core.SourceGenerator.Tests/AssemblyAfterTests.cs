using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class AssemblyAfterTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "AssemblyAfterTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(14);

            await AssertFileContains(generatedFiles[0], 
                """
                new StaticHookMethod<global::TUnit.Core.AssemblyHookContext>
                { 
                    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyBase1).GetMethod("AfterAll1", 0, []),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyBase1.AfterAll1()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 5,
                },
                """);
            
            await AssertFileContains(generatedFiles[2], 
                """
                new StaticHookMethod<global::TUnit.Core.AssemblyHookContext>
                { 
                    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyBase2).GetMethod("AfterAll2", 0, []),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyBase2.AfterAll2()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 20,
                },
                """);
            
            await AssertFileContains(generatedFiles[4], 
                """
                new StaticHookMethod<global::TUnit.Core.AssemblyHookContext>
                { 
                    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyBase3).GetMethod("AfterAll3", 0, []),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyBase3.AfterAll3()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 35,
                },
                """);
            
            await AssertFileContains(generatedFiles[6], 
                """
                            new StaticHookMethod<global::TUnit.Core.AssemblyHookContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyCleanupTests).GetMethod("AfterAllCleanUp", 0, []),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyCleanupTests.AfterAllCleanUp()),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 50,
                            },
                    """);
            
            await AssertFileContains(generatedFiles[7], 
                """
                            new StaticHookMethod<global::TUnit.Core.AssemblyHookContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyCleanupTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.AssemblyHookContext)]),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyCleanupTests.AfterAllCleanUpWithContext(context)),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 56,
                            },
                    """);
            
            await AssertFileContains(generatedFiles[8], 
                """
                            new StaticHookMethod<global::TUnit.Core.AssemblyHookContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyCleanupTests).GetMethod("AfterAllCleanUp", 0, [typeof(global::System.Threading.CancellationToken)]),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyCleanupTests.AfterAllCleanUp(cancellationToken)),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 62,
                            },
                    """);
            
            await AssertFileContains(generatedFiles[9], 
                """
                            new StaticHookMethod<global::TUnit.Core.AssemblyHookContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyCleanupTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.AssemblyHookContext), typeof(global::System.Threading.CancellationToken)]),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyCleanupTests.AfterAllCleanUpWithContext(context, cancellationToken)),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 68,
                            },
                    """);
        });
}