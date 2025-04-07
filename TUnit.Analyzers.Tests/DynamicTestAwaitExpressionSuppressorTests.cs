using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace TUnit.Analyzers.Tests;

public class DynamicTestAwaitExpressionSuppressorTests
{
    private static readonly DiagnosticResult CS4014 = new("CS4014", DiagnosticSeverity.Warning);
    
    [Test]
    public async Task WarningsInTUnitAreSuppressed() =>
        await AnalyzerTestHelpers
            .CreateSuppressorTest<DynamicTestAwaitExpressionSuppressor>(
                $$"""
                using System;
                using System.Threading;
                using System.Threading.Tasks;
                using TUnit;
                using TUnit.Core;
                
                namespace TUnit.TestProject.DynamicTests;
                
                public class Basic
                {
                    public void SomeMethod()
                    {
                        Console.Out.WriteLine(@"Hello, World!");
                    }
                    
                    public async ValueTask SomeMethod_ValueTask()
                    {
                        await default(ValueTask);
                        Console.WriteLine(@"Hello, World!");
                    }
                    
                    public async Task SomeMethod_Task()
                    {
                        await Task.CompletedTask;
                        Console.WriteLine(@"Hello, World!");
                    }
                
                    public void SomeMethod_Args(int a, string b, bool c)
                    {
                        Console.WriteLine(@"Hello, World!");
                    }
                    
                    public async ValueTask SomeMethod_ValueTask_Args(int a, string b, bool c)
                    {
                        await default(ValueTask);
                        Console.WriteLine(@"Hello, World!");
                    }
                    
                    public async Task SomeMethod_Task_Args(int a, string b, bool c)
                    {
                        await Task.CompletedTask;
                        Console.WriteLine(@"Hello, World!");
                    }
                    
                #pragma warning disable TUnitWIP0001
                    [DynamicTestBuilder]
                #pragma warning restore TUnitWIP0001
                    public async Task BuildTests(DynamicTestBuilderContext context)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(0.5));
                        
                        context.AddTest(new DynamicTest<Basic>
                        {
                            TestMethod = @class => @class.SomeMethod(),
                            TestMethodArguments = [],
                            Attributes = [new RepeatAttribute(5)]
                        });
                        
                        context.AddTest(new DynamicTest<Basic>
                        {
                            TestMethod = @class => {|#0:@class.SomeMethod_Task()|},
                            TestMethodArguments = [],
                            Attributes = [new RepeatAttribute(5)]
                        });
                        
                        context.AddTest(new DynamicTest<Basic>
                        {
                            TestMethod = @class => {|#1:@class.SomeMethod_ValueTask()|},
                            TestMethodArguments = [],
                            Attributes = [new RepeatAttribute(5)]
                        });
                        
                        context.AddTest(new DynamicTest<Basic>
                        {
                            TestMethod = @class => @class.SomeMethod_Args(1, "test", true),
                            TestMethodArguments = [2, "test", false],
                            Attributes = [new RepeatAttribute(5)]
                        });
                        
                        context.AddTest(new DynamicTest<Basic>
                        {
                            TestMethod = @class => {|#2:@class.SomeMethod_Task_Args(1, "test", true)|},
                            TestMethodArguments = [2, "test", false],
                            Attributes = [new RepeatAttribute(5)]
                        });
                        
                        context.AddTest(new DynamicTest<Basic>
                        {
                            TestMethod = @class => {|#3:@class.SomeMethod_ValueTask_Args(1, "test", true)|},
                            TestMethodArguments = [2, "test", false],
                            Attributes = [new RepeatAttribute(5)]
                        });
                        
                        context.AddTest(new DynamicTest<Basic>
                        {
                            TestMethod = @class => {|#4:@class.SomeMethod_ValueTask_Args(1, "test", true)|},
                            TestMethodArguments = [2, "test", false],
                            Attributes = [new RepeatAttribute(5)]
                        });
                    }
                }
                """
            )
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .IgnoringDiagnostics("CS1591", "CS8618", "CS8603")
            .WithSpecificDiagnostics(CS4014)
            .WithExpectedDiagnosticsResults(
                CS4014.WithLocation(0).WithIsSuppressed(true),
                CS4014.WithLocation(1).WithIsSuppressed(true),
                CS4014.WithLocation(2).WithIsSuppressed(true),
                CS4014.WithLocation(3).WithIsSuppressed(true),
                CS4014.WithLocation(4).WithIsSuppressed(true)
                )
            .RunAsync();
}