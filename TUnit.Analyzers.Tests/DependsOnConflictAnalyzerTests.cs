using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.DependsOnConflictAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class DependsOnConflictAnalyzerTests
{
    [Test]
    public async Task Direct_Conflict_Raises_Error()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Core;
                            
                            public class MyClass
                            {
                                [Test, DependsOn(nameof(Test2))]
                                public void {|#0:Test|}()
                                {
                                }
                                
                                [Test, DependsOn(nameof(Test))]
                                public void {|#1:Test2|}()
                                {
                                }
                            }
                            """;

        var expected = Verifier
            .Diagnostic(Rules.DependsOnConflicts)
            .WithMessage("DependsOn Conflicts: Test > Test2 > Test")
            .WithLocation(0);
        
        var expected2 = Verifier
            .Diagnostic(Rules.DependsOnConflicts)
            .WithMessage("DependsOn Conflicts: Test2 > Test > Test2")
            .WithLocation(1);
        
        await Verifier.VerifyAnalyzerAsync(text, expected, expected2).ConfigureAwait(false);
    }
    
    [Test]
    public async Task Direct_Conflict_Other_Class_Raises_Error()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Core;

                            [DependsOn(typeof(MyClass2))]
                            public class MyClass1
                            {
                                [Test]
                                public void {|#0:Test|}()
                                {
                                }
                            }
                            
                            [DependsOn(typeof(MyClass1))]
                            public class MyClass2
                            {
                                [Test]
                                public void {|#1:Test2|}()
                                {
                                }
                            }
                            """;

        var expected = Verifier
            .Diagnostic(Rules.DependsOnConflicts)
            .WithMessage("DependsOn Conflicts: Test > Test2 > Test")
            .WithLocation(0);
        
        var expected2 = Verifier
            .Diagnostic(Rules.DependsOnConflicts)
            .WithMessage("DependsOn Conflicts: Test2 > Test > Test2")
            .WithLocation(1);
        
        await Verifier.VerifyAnalyzerAsync(text, expected, expected2).ConfigureAwait(false);
    }
    
    [Test]
    public async Task Not_Found_Test_Raises_Error()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Core;

                            public class MyClass
                            {
                                [Test]
                                public void Test()
                                {
                                }
                                
                                [Test, {|#0:DependsOn("Test3")|}]
                                public void Test2()
                                {
                                }
                            
                            }
                            """;

        var expected = Verifier
            .Diagnostic(Rules.NoMethodFound)
            .WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
    
    [Test]
    public async Task Nested_Conflict_Raises_Error()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Core;

                            public class MyClass
                            {
                                [Test, DependsOn(nameof(Test5))]
                                public void {|#0:Test|}()
                                {
                                }
                                
                                [Test, DependsOn(nameof(Test))]
                                public void {|#1:Test2|}()
                                {
                                }
                                
                                [Test, DependsOn(nameof(Test2))]
                                public void {|#2:Test3|}()
                                {
                                }

                                [Test, DependsOn(nameof(Test3))]
                                public void {|#3:Test4|}()
                                {
                                }

                                [Test, DependsOn(nameof(Test4))]
                                public void {|#4:Test5|}()
                                {
                                }
                            }
                            """;

        var expected = Verifier
            .Diagnostic(Rules.DependsOnConflicts)
            .WithMessage("DependsOn Conflicts: Test > Test5 > Test4 > Test3 > Test2 > Test")
            .WithLocation(0);
        
        var expected2 = Verifier
            .Diagnostic(Rules.DependsOnConflicts)
            .WithMessage("DependsOn Conflicts: Test2 > Test > Test5 > Test4 > Test3 > Test2")
            .WithLocation(1);
        
        var expected3 = Verifier
            .Diagnostic(Rules.DependsOnConflicts)
            .WithMessage("DependsOn Conflicts: Test3 > Test2 > Test > Test5 > Test4 > Test3")
            .WithLocation(2);
        
        var expected4 = Verifier
            .Diagnostic(Rules.DependsOnConflicts)
            .WithMessage("DependsOn Conflicts: Test4 > Test3 > Test2 > Test > Test5 > Test4")
            .WithLocation(3);
        
        var expected5 = Verifier
            .Diagnostic(Rules.DependsOnConflicts)
            .WithMessage("DependsOn Conflicts: Test5 > Test4 > Test3 > Test2 > Test > Test5")
            .WithLocation(4);
        
        await Verifier.VerifyAnalyzerAsync(text, expected, expected2, expected3, expected4, expected5).ConfigureAwait(false);
    }
}