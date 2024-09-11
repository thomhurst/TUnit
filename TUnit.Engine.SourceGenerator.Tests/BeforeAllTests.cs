using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Extensions;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class BeforeAllTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "BeforeTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(14));

            Assert.That(generatedFiles[0].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            ClassHookOrchestrator.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.Base1), new StaticHookMethod<ClassHookContext>
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base1).GetMethod("BeforeAll1", 0, []),
	                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.Base1.BeforeAll1()),
	                HookExecutor = DefaultExecutor.Instance,
	                Order = 0,
	                FilePath = @"", 
	                LineNumber = 5,
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[2].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            ClassHookOrchestrator.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.Base2), new StaticHookMethod<ClassHookContext>
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base2).GetMethod("BeforeAll2", 0, []),
	                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.Base2.BeforeAll2()),
	                HookExecutor = DefaultExecutor.Instance,
	                Order = 0,
	                FilePath = @"", 
	                LineNumber = 20,
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[4].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
	            ClassHookOrchestrator.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.Base3), new StaticHookMethod<ClassHookContext>
	            { 
	                MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.Base3).GetMethod("BeforeAll3", 0, []),
	                Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.Base3.BeforeAll3()),
	                HookExecutor = DefaultExecutor.Instance,
	                Order = 0,
	                FilePath = @"", 
	                LineNumber = 35,
	            });
	            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[6].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.SetupTests), new StaticHookMethod<ClassHookContext>
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeAllSetUp", 0, []),
		                       Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.SetupTests.BeforeAllSetUp()),
		                       HookExecutor = DefaultExecutor.Instance,
		                       Order = 0,
		                       FilePath = @"", 
		                       LineNumber = 50,
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[7].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.SetupTests), new StaticHookMethod<ClassHookContext>
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.ClassHookContext)]),
		                       Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.SetupTests.BeforeAllSetUpWithContext(context)),
		                       HookExecutor = DefaultExecutor.Instance,
		                       Order = 0,
		                       FilePath = @"", 
		                       LineNumber = 56,
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[8].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.SetupTests), new StaticHookMethod<ClassHookContext>
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeAllSetUp", 0, [typeof(global::System.Threading.CancellationToken)]),
		                       Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.SetupTests.BeforeAllSetUp(cancellationToken)),
		                       HookExecutor = DefaultExecutor.Instance,
		                       Order = 0,
		                       FilePath = @"", 
		                       LineNumber = 62,
		            		});
		            """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[9].IgnoreWhitespaceFormatting(), Does.Contain(
	            """
		            		ClassHookOrchestrator.RegisterBeforeHook(typeof(global::TUnit.TestProject.BeforeTests.SetupTests), new StaticHookMethod<ClassHookContext>
		            		{ 
		                       MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeAllSetUpWithContext", 0, [typeof(global::TUnit.Core.ClassHookContext), typeof(global::System.Threading.CancellationToken)]),
		                       Body = (context, cancellationToken) => AsyncConvert.Convert(() => global::TUnit.TestProject.BeforeTests.SetupTests.BeforeAllSetUpWithContext(context, cancellationToken)),
		                       HookExecutor = DefaultExecutor.Instance,
		                       Order = 0,
		                       FilePath = @"", 
		                       LineNumber = 68,
		            		});
		            """.IgnoreWhitespaceFormatting()));
        });
}