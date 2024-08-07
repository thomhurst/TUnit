using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.CodeGenerators.Writers.Hooks;
using TUnit.Engine.SourceGenerator.Tests.Extensions;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class AssemblyAfterTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "AssemblyAfterTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(14));

            Assert.That(generatedFiles[0].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            AssemblyHookOrchestrator.RegisterCleanUp(new StaticMethod
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyBase1).GetMethod("AfterAll1", 0, []),
	                Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyBase1.AfterAll1())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[2].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            AssemblyHookOrchestrator.RegisterCleanUp(new StaticMethod
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyBase2).GetMethod("AfterAll2", 0, []),
	                Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyBase2.AfterAll2())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[4].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            AssemblyHookOrchestrator.RegisterCleanUp(new StaticMethod
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyBase3).GetMethod("AfterAll3", 0, []),
	                Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyBase3.AfterAll3())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[6].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		AssemblyHookOrchestrator.RegisterCleanUp(new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyCleanupTests).GetMethod("AfterAllCleanUp", 0, []),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyCleanupTests.AfterAllCleanUp())
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[7].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		AssemblyHookOrchestrator.RegisterCleanUp(new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyCleanupTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.Models.AssemblyHookContext)]),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyCleanupTests.AfterAllCleanUpWithContext(TUnit.Engine.Hooks.AssemblyHookOrchestrator.GetAssemblyHookContext(typeof(global::TUnit.TestProject.AfterTests.AssemblyCleanupTests))))
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[8].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		AssemblyHookOrchestrator.RegisterCleanUp(new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyCleanupTests).GetMethod("AfterAllCleanUp", 0, [typeof(global::System.Threading.CancellationToken)]),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyCleanupTests.AfterAllCleanUp(cancellationToken))
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[9].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		AssemblyHookOrchestrator.RegisterCleanUp(new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.AfterTests.AssemblyCleanupTests).GetMethod("AfterAllCleanUpWithContext", 0, [typeof(global::TUnit.Core.Models.AssemblyHookContext), typeof(global::System.Threading.CancellationToken)]),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.AfterTests.AssemblyCleanupTests.AfterAllCleanUpWithContext(TUnit.Engine.Hooks.AssemblyHookOrchestrator.GetAssemblyHookContext(typeof(global::TUnit.TestProject.AfterTests.AssemblyCleanupTests)), cancellationToken))
		            		});
		            """.IgnoreWhitespaceFormatting()));
        });
}