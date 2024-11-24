using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class AssemblyBeforeTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "AssemblyBeforeTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(14);

            await AssertFileContains(generatedFiles[0], 
                """
                new BeforeAssemblyHookMethod
                { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblyBase1).GetMethod("BeforeAll1", 0, []),
                    AsyncBody = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblyBase1.BeforeAll1()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 5,
                },
                """);
            
            await AssertFileContains(generatedFiles[2], 
                """
                new BeforeAssemblyHookMethod
                { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblyBase2).GetMethod("BeforeAll2", 0, []),
                    AsyncBody = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblyBase2.BeforeAll2()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 20,
                },
                """);
            
            await AssertFileContains(generatedFiles[4], 
                """
                new BeforeAssemblyHookMethod
                { 
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblyBase3).GetMethod("BeforeAll3", 0, []),
                    AsyncBody = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblyBase3.BeforeAll3()),
                    HookExecutor = DefaultExecutor.Instance,
                    Order = 0,
                    FilePath = @"", 
                    LineNumber = 35,
                },
                """);
            
            await AssertFileContains(generatedFiles[6], 
                """
                            new BeforeAssemblyHookMethod
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblySetupTests).GetMethod("BeforeAllSetUp", 0, []),
                               AsyncBody = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblySetupTests.BeforeAllSetUp()),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 50,
                            },
                    """);
            
            await AssertFileContains(generatedFiles[7], 
                """
                            new BeforeAssemblyHookMethod
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblySetupTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.AssemblyHookContext)]),
                               AsyncBody = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblySetupTests.BeforeAllSetUpWithContext(context)),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 56,
                            },
                    """);
            
            await AssertFileContains(generatedFiles[8], 
                """
                            new BeforeAssemblyHookMethod
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblySetupTests).GetMethod("BeforeAllSetUp", 0, [typeof(global::System.Threading.CancellationToken)]),
                               AsyncBody = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblySetupTests.BeforeAllSetUp(cancellationToken)),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 62,
                            },
                    """);
            
            await AssertFileContains(generatedFiles[9], 
                """
                            new BeforeAssemblyHookMethod
                            { 
                               MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblySetupTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.AssemblyHookContext), typeof(global::System.Threading.CancellationToken)]),
                               AsyncBody = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblySetupTests.BeforeAllSetUpWithContext(context, cancellationToken)),
                               HookExecutor = DefaultExecutor.Instance,
                               Order = 0,
                               FilePath = @"", 
                               LineNumber = 68,
                            },
                    """);
        });
}