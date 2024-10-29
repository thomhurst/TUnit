using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.DependsOnConflictAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class DependsOnConflictAnalyzerTests
{
    [Test]
    public async Task Direct_Conflict_Raises_Error()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
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
            """,

            Verifier
                .Diagnostic(Rules.DependsOnConflicts)
                .WithMessage("DependsOn Conflicts: MyClass.Test > MyClass.Test2 > MyClass.Test")
                .WithLocation(0),

            Verifier
                .Diagnostic(Rules.DependsOnConflicts)
                .WithMessage("DependsOn Conflicts: MyClass.Test2 > MyClass.Test > MyClass.Test2")
                .WithLocation(1)
        );
    }
    
    [Test]
    public async Task Direct_Conflict_Other_Class_Raises_Error()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;
            using TUnit.Core;

            [DependsOn(typeof(MyClass2))]
            public class MyClass1
            {
                [Test]
                public void {|#0:Test|}()
                {
                }

                [Test]
                public void {|#1:Test2|}()
                {
                }
            }
                            
            [DependsOn(typeof(MyClass1))]
            public class MyClass2
            {
                [Test]
                public void {|#2:Test|}()
                {
                }
                                                                
                [Test]
                public void {|#3:Test2|}()
                {
                }
            }
            """,

            Verifier
                .Diagnostic(Rules.DependsOnConflicts)
                .WithMessage("DependsOn Conflicts: MyClass1.Test > MyClass2.Test > MyClass1.Test")
                .WithLocation(0),

            Verifier
                .Diagnostic(Rules.DependsOnConflicts)
                .WithMessage("DependsOn Conflicts: MyClass2.Test > MyClass1.Test > MyClass2.Test")
                .WithLocation(2)
        );
    }
    
    [Test]
    public async Task Direct_Conflict_Other_Class_Raises_Error2()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass1
                {
                    [DependsOn(typeof(MyClass2), nameof(MyClass2.Test2))]
                    [Test]
                    public void {|#0:Test|}()
                    {
                    }
                }

                public class MyClass2
                {
                    [DependsOn(typeof(MyClass1), nameof(MyClass1.Test))]
                    [Test]
                    public void {|#1:Test2|}()
                    {
                    }
                }
                """,
                
                Verifier
                    .Diagnostic(Rules.DependsOnConflicts)
                    .WithMessage("DependsOn Conflicts: MyClass1.Test > MyClass2.Test2 > MyClass1.Test")
                    .WithLocation(0),
                
                Verifier
                    .Diagnostic(Rules.DependsOnConflicts)
                    .WithMessage("DependsOn Conflicts: MyClass2.Test2 > MyClass1.Test > MyClass2.Test2")
                    .WithLocation(1)
            );
    }
    
    [Test]
    public async Task Not_Found_Test_Raises_Error()
    {
        await Verifier
			.VerifyAnalyzerAsync(
				"""
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
                """,

                Verifier
                    .Diagnostic(Rules.NoMethodFound)
                    .WithLocation(0)
            );
    }
    
    [Test]
    public async Task Nested_Conflict_Raises_Error()
    {
        await Verifier
			.VerifyAnalyzerAsync(
				"""
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
                """,

                Verifier
                    .Diagnostic(Rules.DependsOnConflicts)
                    .WithMessage("DependsOn Conflicts: MyClass.Test > MyClass.Test5 > MyClass.Test4 > MyClass.Test3 > MyClass.Test2 > MyClass.Test")
                    .WithLocation(0),

                Verifier
                    .Diagnostic(Rules.DependsOnConflicts)
                    .WithMessage("DependsOn Conflicts: MyClass.Test2 > MyClass.Test > MyClass.Test5 > MyClass.Test4 > MyClass.Test3 > MyClass.Test2")
                    .WithLocation(1),

                Verifier
                    .Diagnostic(Rules.DependsOnConflicts)
                    .WithMessage("DependsOn Conflicts: MyClass.Test3 > MyClass.Test2 > MyClass.Test > MyClass.Test5 > MyClass.Test4 > MyClass.Test3")
                    .WithLocation(2),

                Verifier
                    .Diagnostic(Rules.DependsOnConflicts)
                    .WithMessage("DependsOn Conflicts: MyClass.Test4 > MyClass.Test3 > MyClass.Test2 > MyClass.Test > MyClass.Test5 > MyClass.Test4")
                    .WithLocation(3),

                Verifier
                    .Diagnostic(Rules.DependsOnConflicts)
                    .WithMessage("DependsOn Conflicts: MyClass.Test5 > MyClass.Test4 > MyClass.Test3 > MyClass.Test2 > MyClass.Test > MyClass.Test5")
                    .WithLocation(4)
            );
    }
}