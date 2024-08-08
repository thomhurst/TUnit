using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Extensions;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class AssemblyBeforeTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "AssemblyBeforeTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(14));

            Assert.That(generatedFiles[0].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            AssemblyHookOrchestrator.RegisterSetUp(new StaticMethod
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblyBase1).GetMethod("BeforeAll1", 0, []),
	                Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblyBase1.BeforeAll1())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[2].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            AssemblyHookOrchestrator.RegisterSetUp(new StaticMethod
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblyBase2).GetMethod("BeforeAll2", 0, []),
	                Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblyBase2.BeforeAll2())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[4].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            AssemblyHookOrchestrator.RegisterSetUp(new StaticMethod
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblyBase3).GetMethod("BeforeAll3", 0, []),
	                Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblyBase3.BeforeAll3())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[6].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		AssemblyHookOrchestrator.RegisterSetUp(new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblySetupTests).GetMethod("BeforeAllSetUp", 0, []),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblySetupTests.BeforeAllSetUp())
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[7].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		AssemblyHookOrchestrator.RegisterSetUp(new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblySetupTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.AssemblyHookContext)]),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblySetupTests.BeforeAllSetUpWithContext(TUnit.Engine.Hooks.AssemblyHookOrchestrator.GetAssemblyHookContext(typeof(global::TUnit.TestProject.BeforeTests.AssemblySetupTests))))
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[8].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		AssemblyHookOrchestrator.RegisterSetUp(new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblySetupTests).GetMethod("BeforeAllSetUp", 0, [typeof(global::System.Threading.CancellationToken)]),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblySetupTests.BeforeAllSetUp(cancellationToken))
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[9].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		AssemblyHookOrchestrator.RegisterSetUp(new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.AssemblySetupTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.AssemblyHookContext), typeof(global::System.Threading.CancellationToken)]),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.AssemblySetupTests.BeforeAllSetUpWithContext(TUnit.Engine.Hooks.AssemblyHookOrchestrator.GetAssemblyHookContext(typeof(global::TUnit.TestProject.BeforeTests.AssemblySetupTests)), cancellationToken))
		            		});
		            """.IgnoreWhitespaceFormatting()));
        });
}