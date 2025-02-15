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
    public async Task Direct_Conflict_Other_Class_Raises_Error_GenericAttribute()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;
            using TUnit.Core;

            [DependsOn<MyClass2>]
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
                            
            [DependsOn<MyClass1>]
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
    public async Task No_Conflict_Raises_Nothing_GenericAttribute()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;
            using TUnit.Core;

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
                            
            [DependsOn<MyClass1>]
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
            """
        );
    }
        
    [Test]
    public async Task No_Conflict_Raises_Nothing()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;
            using TUnit.Core;

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
            """
        );
    }
    
    [Test]
    public async Task No_Conflict_Base_Class_Raises_Nothing()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;
            using TUnit.Core;

            public class MyClass : BaseClass
            {
                [DependsOn(nameof(BaseTest))]
                [Test]
                public void SubTypeTest()
                {
                }
            }
                            
            public class BaseClass
            {
                [Test]
                public void BaseTest()
                {
                }
            }
            """
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
    
    [Test]
    public async Task Deep_Nested_With_Direct_Conflict()
    {
        await Verifier
			.VerifyAnalyzerAsync(
				"""
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [DependsOn(nameof(Test2))]
                    public void Test1()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test3))]
                    public void Test2()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test4))]
                    public void Test3()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test5))]
                    public void Test4()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test6))]
                    public void Test5()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test7))]
                    public void Test6()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test8))]
                    public void Test7()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test9))]
                    public void Test8()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test10))]
                    public void {|#0:Test9|}()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test9))]
                    public void {|#1:Test10|}()
                    {
                    }
                }
                """,

                Verifier
                    .Diagnostic(Rules.DependsOnConflicts)
                    .WithMessage("DependsOn Conflicts: MyClass.Test9 > MyClass.Test10 > MyClass.Test9")
                    .WithLocation(0),
                
                Verifier
                    .Diagnostic(Rules.DependsOnConflicts)
                    .WithMessage("DependsOn Conflicts: MyClass.Test10 > MyClass.Test9 > MyClass.Test10")
                    .WithLocation(1)
            );
    }
    
    [Test]
    public async Task Deep_Nested_With_Indirect_Conflict()
    {
        await Verifier
			.VerifyAnalyzerAsync(
				"""
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [DependsOn(nameof(Test2))]
                    public void Test1()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test3))]
                    public void Test2()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test4))]
                    public void Test3()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test5))]
                    public void Test4()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test6))]
                    public void Test5()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test7))]
                    public void Test6()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test8))]
                    public void Test7()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test9))]
                    public void {|#0:Test8|}()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test10))]
                    public void {|#1:Test9|}()
                    {
                    }
                    
                    [Test]
                    [DependsOn(nameof(Test8))]
                    public void {|#2:Test10|}()
                    {
                    }
                }
                """,

                Verifier
                    .Diagnostic(Rules.DependsOnConflicts)
                    .WithMessage("DependsOn Conflicts: MyClass.Test8 > MyClass.Test9 > MyClass.Test10 > MyClass.Test8")
                    .WithLocation(0),
                
                Verifier
                    .Diagnostic(Rules.DependsOnConflicts)
                    .WithMessage("DependsOn Conflicts: MyClass.Test9 > MyClass.Test10 > MyClass.Test8 > MyClass.Test9")
                    .WithLocation(1),
                
                Verifier
                    .Diagnostic(Rules.DependsOnConflicts)
                    .WithMessage("DependsOn Conflicts: MyClass.Test10 > MyClass.Test8 > MyClass.Test9 > MyClass.Test10")
                    .WithLocation(2)
            );
    }
}