using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class AfterAllTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "AfterTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(14);

            await AssertFileContains(generatedFiles[0], 
                """
                TestRegistrar.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.Base1), new StaticHookMethod<ClassHookContext>
                { 
                    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base1).GetMethod("AfterAll1", 0, []),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.Base1.AfterAll1()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0, 
                    FilePath = @"", 
                    LineNumber = 5,
                });
                """);
            
            await AssertFileContains(generatedFiles[2], 
                """
                TestRegistrar.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.Base2), new StaticHookMethod<ClassHookContext>
                { 
                    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base2).GetMethod("AfterAll2", 0, []),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.Base2.AfterAll2()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 20,
                });
                """);
            
            await AssertFileContains(generatedFiles[4], 
                """
                TestRegistrar.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.Base3), new StaticHookMethod<ClassHookContext>
                { 
                    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base3).GetMethod("AfterAll3", 0, []),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.Base3.AfterAll3()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 35,
                });
                """);
            
            await AssertFileContains(generatedFiles[6], 
                """
                            TestRegistrar.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.CleanupTests), new StaticHookMethod<ClassHookContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterAllCleanUp", 0, []),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.CleanupTests.AfterAllCleanUp()),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 50,
                            });
                    """);
            
            await AssertFileContains(generatedFiles[7], 
                """
                            TestRegistrar.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.CleanupTests), new StaticHookMethod<ClassHookContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.ClassHookContext)]),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.CleanupTests.AfterAllCleanUpWithContext(context)),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 56,
                            });
                    """);
            
            await AssertFileContains(generatedFiles[8], 
                """
                            TestRegistrar.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.CleanupTests), new StaticHookMethod<ClassHookContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterAllCleanUp", 0, [typeof(global::System.Threading.CancellationToken)]),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.CleanupTests.AfterAllCleanUp(cancellationToken)),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 62,
                            });
                    """);
            
            await AssertFileContains(generatedFiles[9], 
                """
                            TestRegistrar.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.CleanupTests), new StaticHookMethod<ClassHookContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.ClassHookContext), typeof(global::System.Threading.CancellationToken)]),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.CleanupTests.AfterAllCleanUpWithContext(context, cancellationToken)),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 68,
                            });
                    """);
        });
}