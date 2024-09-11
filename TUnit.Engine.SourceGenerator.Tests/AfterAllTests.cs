using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Extensions;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class AfterAllTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "AfterTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(14));

            Assert.That(generatedFiles[0].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            ClassHookOrchestrator.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.Base1), new StaticHookMethod<ClassHookContext>
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base1).GetMethod("AfterAll1", 0, []),
	                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.Base1.AfterAll1()),
	                HookExecutor = DefaultExecutor.Instance,
	                Order = 0, 
	                FilePath = @"", 
	                LineNumber = 5,
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[2].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            ClassHookOrchestrator.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.Base2), new StaticHookMethod<ClassHookContext>
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base2).GetMethod("AfterAll2", 0, []),
	                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.Base2.AfterAll2()),
	                HookExecutor = DefaultExecutor.Instance,
	                Order = 0,
	                FilePath = @"", 
	                LineNumber = 20,
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[4].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            ClassHookOrchestrator.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.Base3), new StaticHookMethod<ClassHookContext>
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base3).GetMethod("AfterAll3", 0, []),
	                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.Base3.AfterAll3()),
	                HookExecutor = DefaultExecutor.Instance,
	                Order = 0,
	                FilePath = @"", 
	                LineNumber = 35,
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[6].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.CleanupTests), new StaticHookMethod<ClassHookContext>
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterAllCleanUp", 0, []),
		                       Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.CleanupTests.AfterAllCleanUp()),
		                       HookExecutor = DefaultExecutor.Instance,
		                       Order = 0,
		                       FilePath = @"", 
		                       LineNumber = 50,
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[7].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.CleanupTests), new StaticHookMethod<ClassHookContext>
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.ClassHookContext)]),
		                       Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.CleanupTests.AfterAllCleanUpWithContext(context)),
		                       HookExecutor = DefaultExecutor.Instance,
		                       Order = 0,
		                       FilePath = @"", 
		                       LineNumber = 56,
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[8].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.CleanupTests), new StaticHookMethod<ClassHookContext>
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterAllCleanUp", 0, [typeof(global::System.Threading.CancellationToken)]),
		                       Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.CleanupTests.AfterAllCleanUp(cancellationToken)),
		                       HookExecutor = DefaultExecutor.Instance,
		                       Order = 0,
		                       FilePath = @"", 
		                       LineNumber = 62,
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[9].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterAfterHook(typeof(global::TUnit.TestProject.AfterTests.CleanupTests), new StaticHookMethod<ClassHookContext>
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.ClassHookContext), typeof(global::System.Threading.CancellationToken)]),
		                       Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.CleanupTests.AfterAllCleanUpWithContext(context, cancellationToken)),
		                       HookExecutor = DefaultExecutor.Instance,
		                       Order = 0,
		                       FilePath = @"", 
		                       LineNumber = 68,
		            		});
		            """.IgnoreWhitespaceFormatting()));
        });
}