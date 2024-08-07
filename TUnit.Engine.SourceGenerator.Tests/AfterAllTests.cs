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
	            ClassHookOrchestrator.RegisterCleanUp(typeof(global::TUnit.TestProject.AfterTests.Base1), new StaticMethod
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base1).GetMethod("AfterAll1", 0, []),
	                Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.Base1.AfterAll1())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[2].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            ClassHookOrchestrator.RegisterCleanUp(typeof(global::TUnit.TestProject.AfterTests.Base2), new StaticMethod
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base2).GetMethod("AfterAll2", 0, []),
	                Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.Base2.AfterAll2())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[4].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            ClassHookOrchestrator.RegisterCleanUp(typeof(global::TUnit.TestProject.AfterTests.Base3), new StaticMethod
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.AfterTests.Base3).GetMethod("AfterAll3", 0, []),
	                Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.Base3.AfterAll3())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[6].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterCleanUp(typeof(global::TUnit.TestProject.AfterTests.CleanupTests), new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterAllCleanUp", 0, []),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.CleanupTests.AfterAllCleanUp())
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[7].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterCleanUp(typeof(global::TUnit.TestProject.AfterTests.CleanupTests), new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.Models.ClassHookContext)]),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.CleanupTests.AfterAllCleanUpWithContext(TUnit.Engine.Hooks.ClassHookOrchestrator.GetClassHookContext(typeof(global::TUnit.TestProject.AfterTests.CleanupTests))))
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[8].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterCleanUp(typeof(global::TUnit.TestProject.AfterTests.CleanupTests), new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterAllCleanUp", 0, [typeof(global::System.Threading.CancellationToken)]),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.CleanupTests.AfterAllCleanUp(cancellationToken))
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[9].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterCleanUp(typeof(global::TUnit.TestProject.AfterTests.CleanupTests), new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.Models.ClassHookContext), typeof(global::System.Threading.CancellationToken)]),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.CleanupTests.AfterAllCleanUpWithContext(TUnit.Engine.Hooks.ClassHookOrchestrator.GetClassHookContext(typeof(global::TUnit.TestProject.AfterTests.CleanupTests)), cancellationToken))
		            		});
		            """.IgnoreWhitespaceFormatting()));
        });
}