using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.GlobalTestHooksAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class GlobalTestHooksAnalyzerTests
{
    [Test]
    [Arguments("TestDiscovery", "BeforeTestDiscoveryContext context")]
    [Arguments("TestDiscovery", "")]
    [Arguments("TestSession", "TestSessionContext context")]
    [Arguments("TestSession", "")]
    [Arguments("Test", "TestContext context")]
    [Arguments("Class", "ClassHookContext context")]
    [Arguments("Assembly", "AssemblyHookContext context")]
    public async Task Before_No_Error(string hookType, string parameter)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;
                using static TUnit.Core.HookType;
                            
                public class Tests
                {
                    [BeforeEvery({{hookType}})]
                    public static void SetUp({{parameter}})
                    {
                    }
                }
                """
            );
    }

    [Test]
    [Arguments("TestDiscovery", "TestDiscoveryContext context")]
    [Arguments("TestDiscovery", "")]
    [Arguments("TestSession", "TestSessionContext context")]
    [Arguments("TestSession", "")]
    [Arguments("Test", "TestContext context")]
    [Arguments("Class", "ClassHookContext context")]
    [Arguments("Assembly", "AssemblyHookContext context")]
    public async Task After_No_Error(string hookType, string parameter)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;
                using static TUnit.Core.HookType;
                     
                public class Tests
                {
                    [AfterEvery({{hookType}})]
                    public static void CleanUp({{parameter}})
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task BeforeEvery_Test_NoParameters_Info()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using static TUnit.Core.HookType;
                     
                public class Tests
                {
                    [BeforeEvery(Test)]
                    public static void {|#0:SetUp|}()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.HookContextParameterOptional)
                    .WithLocation(0)
                    .WithArguments("TestContext")
            );
    }

    [Test]
    public async Task BeforeEvery_Test_UnknownParameters_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using static TUnit.Core.HookType;
                     
                public class Tests
                {
                    [BeforeEvery(Test)]
                    public static void {|#0:SetUp|}(string unknown)
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.HookUnknownParameters)
                    .WithLocation(0)
                    .WithArguments("TestContext")
            );
    }

    [Test]
    public async Task BeforeEvery_Class_NoParameters_Info()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using static TUnit.Core.HookType;
                     
                public class Tests
                {
                    [BeforeEvery(Class)]
                    public static void {|#0:SetUp|}()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.HookContextParameterOptional)
                    .WithLocation(0)
                    .WithArguments("ClassHookContext")
            );
    }

    [Test]
    public async Task BeforeEvery_Test_WithCancellationToken_NoError()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Threading;
                using static TUnit.Core.HookType;
                     
                public class Tests
                {
                    [BeforeEvery(Test)]
                    public static void SetUp(TestContext context, CancellationToken token)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task BeforeEvery_Class_WithCancellationToken_NoError()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Threading;
                using static TUnit.Core.HookType;
                     
                public class Tests
                {
                    [BeforeEvery(Class)]
                    public static void SetUp(ClassHookContext context, CancellationToken token)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task BeforeEvery_Assembly_NoParameters_Info()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                  using TUnit.Core;
                  using static TUnit.Core.HookType;
                       
                  public class Tests
                  {
                      [BeforeEvery(Assembly)]
                      public static void {|#0:SetUp|}()
                      {
                      }
                  }
                  """,
                Verifier.Diagnostic(Rules.HookContextParameterOptional)
                    .WithLocation(0)
                    .WithArguments("AssemblyHookContext")
            );
    }

    [Test]
    public async Task AfterEvery_Test_NoParameters_Info()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using static TUnit.Core.HookType;
                     
                public class Tests
                {
                    [AfterEvery(Test)]
                    public static void {|#0:CleanUp|}()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.HookContextParameterOptional)
                    .WithLocation(0)
                    .WithArguments("TestContext")
            );
    }

    [Test]
    public async Task AfterEvery_Class_NoParameters_Info()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using static TUnit.Core.HookType;
                     
                public class Tests
                {
                    [AfterEvery(Class)]
                    public static void {|#0:CleanUp|}()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.HookContextParameterOptional)
                    .WithLocation(0)
                    .WithArguments("ClassHookContext")
            );
    }

    [Test]
    public async Task AfterEvery_Assembly_NoParameters_Info()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using static TUnit.Core.HookType;
                     
                public class Tests
                {
                    [AfterEvery(Assembly)]
                    public static void {|#0:CleanUp|}()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.HookContextParameterOptional)
                    .WithLocation(0)
                    .WithArguments("AssemblyHookContext")
            );
    }

    [Test]
    public async Task AfterEvery_Test_UnknownParameters_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using static TUnit.Core.HookType;
                     
                public class Tests
                {
                    [AfterEvery(Test)]
                    public static void {|#0:CleanUp|}(string unknown)
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.HookUnknownParameters)
                    .WithLocation(0)
                    .WithArguments("TestContext")
            );
    }

    [Test]
    public async Task AfterEvery_Class_UnknownParameters_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using static TUnit.Core.HookType;
                     
                public class Tests
                {
                    [AfterEvery(Class)]
                    public static void {|#0:CleanUp|}(int unknown1, string unknown2)
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.HookUnknownParameters)
                    .WithLocation(0)
                    .WithArguments("ClassHookContext")
            );
    }

    [Test]
    public async Task AfterEvery_Assembly_UnknownParameters_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using static TUnit.Core.HookType;
                     
                public class Tests
                {
                    [AfterEvery(Assembly)]
                    public static void {|#0:CleanUp|}(object unknown)
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.HookUnknownParameters)
                    .WithLocation(0)
                    .WithArguments("AssemblyHookContext")
            );
    }

    [Test]
    [Arguments("TestDiscovery", "BeforeTestDiscoveryContext")]
    [Arguments("TestSession", "TestSessionContext")]
    [Arguments("Test", "TestContext")]
    [Arguments("Class", "ClassHookContext")]
    [Arguments("Assembly", "AssemblyHookContext")]
    public async Task BeforeEvery_SeparateClass_Error(string hookType, string parameterType)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                  using TUnit.Core;
                  using static TUnit.Core.HookType;
                       
                  public class Tests
                  {
                      [BeforeEvery({{hookType}})]
                      public static void {|#0:MyHook|}({{parameterType}} context)
                      {
                      }
                      
                      [Test]
                      public void MyTest()
                      {
                      }
                  }
                  """,
                Verifier.Diagnostic(Rules.GlobalHooksSeparateClass)
                    .WithLocation(0)
            );
    }

    [Test]
    [Arguments("TestDiscovery", "TestDiscoveryContext")]
    [Arguments("TestSession", "TestSessionContext")]
    [Arguments("Test", "TestContext")]
    [Arguments("Class", "ClassHookContext")]
    [Arguments("Assembly", "AssemblyHookContext")]
    public async Task AfterEvery_SeparateClass_Error(string hookType, string parameterType)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                  using TUnit.Core;
                  using static TUnit.Core.HookType;
                       
                  public class Tests
                  {
                      [AfterEvery({{hookType}})]
                      public static void {|#0:MyHook|}({{parameterType}} context)
                      {
                      }
                      
                      [Test]
                      public void MyTest()
                      {
                      }
                  }
                  """,
                Verifier.Diagnostic(Rules.GlobalHooksSeparateClass)
                    .WithLocation(0)
            );
    }

    [Test]
    [Arguments("TestDiscovery", "BeforeTestDiscoveryContext")]
    [Arguments("TestSession", "TestSessionContext")]
    [Arguments("Assembly", "AssemblyHookContext")]
    public async Task Before_SeparateClass_Error(string hookType, string parameterType)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                  using TUnit.Core;
                  using static TUnit.Core.HookType;
                       
                  public class Tests
                  {
                      [Before({{hookType}})]
                      public static void {|#0:MyHook|}({{parameterType}} context)
                      {
                      }
                      
                      [Test]
                      public void MyTest()
                      {
                      }
                  }
                  """,
                Verifier.Diagnostic(Rules.GlobalHooksSeparateClass)
                    .WithLocation(0)
            );
    }

    [Test]
    [Arguments("TestDiscovery", "TestDiscoveryContext")]
    [Arguments("TestSession", "TestSessionContext")]
    [Arguments("Assembly", "AssemblyHookContext")]
    public async Task After_SeparateClass_Error(string hookType, string parameterType)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                  using TUnit.Core;
                  using static TUnit.Core.HookType;
                       
                  public class Tests
                  {
                      [After({{hookType}})]
                      public static void {|#0:MyHook|}({{parameterType}} context)
                      {
                      }
                      
                      [Test]
                      public void MyTest()
                      {
                      }
                  }
                  """,
                Verifier.Diagnostic(Rules.GlobalHooksSeparateClass)
                    .WithLocation(0)
            );
    }
}
