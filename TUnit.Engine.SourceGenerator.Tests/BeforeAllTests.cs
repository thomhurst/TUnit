using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class BeforeAllTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "BeforeTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(14);

            await AssertFileContains(generatedFiles[0], 
                """
                TestRegistrar.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.Base1), new StaticHookMethod<ClassHookContext>
                { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base1).GetMethod("BeforeAll1", 0, []),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.Base1.BeforeAll1()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 5,
                });
                """);
            
            await AssertFileContains(generatedFiles[2], 
                """
                TestRegistrar.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.Base2), new StaticHookMethod<ClassHookContext>
                { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base2).GetMethod("BeforeAll2", 0, []),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.Base2.BeforeAll2()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 20,
                });
                """);
            
            await AssertFileContains(generatedFiles[4], 
                """
                TestRegistrar.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.Base3), new StaticHookMethod<ClassHookContext>
                { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base3).GetMethod("BeforeAll3", 0, []),
                    Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.Base3.BeforeAll3()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 35,
                });
                """);
            
            await AssertFileContains(generatedFiles[6], 
                """
                            TestRegistrar.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.SetupTests), new StaticHookMethod<ClassHookContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeAllSetUp", 0, []),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.SetupTests.BeforeAllSetUp()),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 50,
                            });
                    """);
            
            await AssertFileContains(generatedFiles[7], 
                """
                            TestRegistrar.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.SetupTests), new StaticHookMethod<ClassHookContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.ClassHookContext)]),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.SetupTests.BeforeAllSetUpWithContext(context)),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 56,
                            });
                    """);
            
            await AssertFileContains(generatedFiles[8], 
                """
                            TestRegistrar.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.SetupTests), new StaticHookMethod<ClassHookContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeAllSetUp", 0, [typeof(global::System.Threading.CancellationToken)]),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.SetupTests.BeforeAllSetUp(cancellationToken)),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 62,
                            });
                    """);
            
            await AssertFileContains(generatedFiles[9], 
                """
                            TestRegistrar.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.SetupTests), new StaticHookMethod<ClassHookContext>
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.ClassHookContext), typeof(global::System.Threading.CancellationToken)]),
                               Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.SetupTests.BeforeAllSetUpWithContext(context, cancellationToken)),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 68,
                            });
                    """);
        });
}