using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Extensions;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class BeforeAllTests : TestsBase<ClassHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "BeforeTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(7));

            Assert.That(generatedFiles[0].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            ClassHookOrchestrator.RegisterSetUp(typeof(global::TUnit.TestProject.BeforeTests.Base1), new StaticMethod
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base1).GetMethod("BeforeAll1", 0, []),
	                Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.Base1.BeforeAll1())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[1].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            ClassHookOrchestrator.RegisterSetUp(typeof(global::TUnit.TestProject.BeforeTests.Base2), new StaticMethod
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base2).GetMethod("BeforeAll2", 0, []),
	                Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.Base2.BeforeAll2())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[2].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            ClassHookOrchestrator.RegisterSetUp(typeof(global::TUnit.TestProject.BeforeTests.Base3), new StaticMethod
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base3).GetMethod("BeforeAll3", 0, []),
	                Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.Base3.BeforeAll3())
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[3].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterSetUp(typeof(global::TUnit.TestProject.BeforeTests.SetupTests), new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeAllSetUp", 0, []),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.SetupTests.BeforeAllSetUp())
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[4].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterSetUp(typeof(global::TUnit.TestProject.BeforeTests.SetupTests), new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.Models.ClassHookContext)]),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.SetupTests.BeforeAllSetUpWithContext(TUnit.Engine.Hooks.ClassHookOrchestrator.GetClassHookContext(typeof(global::TUnit.TestProject.BeforeTests.SetupTests))))
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[5].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterSetUp(typeof(global::TUnit.TestProject.BeforeTests.SetupTests), new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeAllSetUp", 0, [typeof(global::System.Threading.CancellationToken)]),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.SetupTests.BeforeAllSetUp(cancellationToken))
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[6].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterSetUp(typeof(global::TUnit.TestProject.BeforeTests.SetupTests), new StaticMethod
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.Models.ClassHookContext), typeof(global::System.Threading.CancellationToken)]),
		                       Body = cancellationToken => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.SetupTests.BeforeAllSetUpWithContext(TUnit.Engine.Hooks.ClassHookOrchestrator.GetClassHookContext(typeof(global::TUnit.TestProject.BeforeTests.SetupTests)), cancellationToken))
		            		});
		            """.IgnoreWhitespaceFormatting()));
        });
}