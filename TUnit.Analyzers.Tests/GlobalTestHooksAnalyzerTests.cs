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
    public async Task BeforeEvery_Test_Error()
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
                Verifier.Diagnostic(Rules.SingleTestContextParameterRequired)
                    .WithLocation(0)
            );
    }
    
    [Test]
    public async Task BeforeEvery_Class_Error()
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
                Verifier.Diagnostic(Rules.SingleClassHookContextParameterRequired)
                    .WithLocation(0)
            );
    }
    
    [Test]
    public async Task BeforeEvery_Assembly_Error()
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
                Verifier.Diagnostic(Rules.SingleAssemblyHookContextParameterRequired)
                    .WithLocation(0)
            );
    }
    
    [Test]
    public async Task AfterEvery_Test_Error()
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
                Verifier.Diagnostic(Rules.SingleTestContextParameterRequired)
                    .WithLocation(0)
            );
    }
    
    [Test]
    public async Task AfterEvery_Class_Error()
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
                Verifier.Diagnostic(Rules.SingleClassHookContextParameterRequired)
                    .WithLocation(0)
            );
    }
    
    [Test]
    public async Task AfterEvery_Assembly_Error()
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
                Verifier.Diagnostic(Rules.SingleAssemblyHookContextParameterRequired)
                    .WithLocation(0)
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