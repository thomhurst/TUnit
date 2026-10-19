using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.PublicMethodMissingTestAttributeAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class PublicMethodMissingTestAttributeAnalyzerTests
{
    [Test]
    public async Task Class_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                        Helper();
                    }
                                
                    private void Helper()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Class_Missing_Parameter_Error()
    {
        var expected = Verifier.Diagnostic(Rules.PublicMethodMissingTestAttribute).WithLocation(0);

        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                            
                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                        Helper();
                    }
                                
                    public void {|#0:Helper|}()
                    {
                    }
                }
                """,
                expected
            );
    }

    [Test]
    public async Task Before_Hook_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                                
                    [Before(HookType.Test)]
                    public void SetUp()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task After_Hook_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                                
                    [After(HookType.Test)]
                    public void SetUp()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task IDisposable_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using TUnit.Core;

                public class MyClass : IDisposable
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                                
                    public void Dispose()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task IAsyncDisposable_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass : IAsyncDisposable
                {
                    [Test]
                    public void MyTest()
                    {
                    }

                    public ValueTask DisposeAsync()
                    {
                        return ValueTask.CompletedTask;
                    }
                }
                """
            );
    }

    [Test]
    public async Task Interface_Implementation_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Core.Interfaces;

                public interface IGreeter<T>
                {
                    T Greet();
                }

                public class MyClass : ITestStartEventReceiver, IGreeter<string>
                {
                    public int Order => 0;

                    [Test]
                    public void MyTest()
                    {
                    }

                    public ValueTask OnTestStart(TestContext context)
                    {
                        return default;
                    }

                    public string Greet()
                    {
                        return "Hello";
                    }
                }
                """
            );
    }

    [Test]
    public async Task Same_Name_As_Interface_Member_But_Not_Implementing_It_Error()
    {
        var expected = Verifier.Diagnostic(Rules.PublicMethodMissingTestAttribute).WithLocation(0);

        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public interface IGreeter
                {
                    string Greet();
                }

                public class MyClass : IGreeter
                {
                    [Test]
                    public void MyTest()
                    {
                    }

                    string IGreeter.Greet()
                    {
                        return "Hello";
                    }

                    public string {|#0:Greet|}(string name)
                    {
                        return name;
                    }
                }
                """,
                expected
            );
    }

    [Test]
    public async Task Property_Getter_Setter_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }

                    public string? Name { get; set; }

                    public int Age
                    {
                        get { return 0; }
                        set { }
                    }
                }
                """
            );
    }

    [Test]
    public async Task Event_Accessors_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }

                    public event EventHandler MyEvent
                    {
                        add { }
                        remove { }
                    }
                }
                """
            );
    }

    [Test]
    public async Task Override_Method_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }

                    public override string ToString()
                    {
                        return "test";
                    }
                }
                """
            );
    }

    [Test]
    public async Task Static_Method_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }

                    public static void StaticHelper()
                    {
                    }
                }
                """
            );
    }
}
