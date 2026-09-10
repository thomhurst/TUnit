using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.VirtualHookOverrideAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class VirtualHookOverrideAnalyzerTests
{
    [Test]
    public async Task Before_Test_On_Both_Base_And_Override_Reports_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;
                using static TUnit.Core.HookType;

                public class BaseClass
                {
                    [Before(Test)]
                    public virtual Task SetupAsync() => Task.CompletedTask;
                }

                public class DerivedClass : BaseClass
                {
                    [{|#0:Before(Test)|}]
                    public override Task SetupAsync() => base.SetupAsync();
                }
                """,
                Verifier.Diagnostic(Rules.RedundantHookAttributeOnOverride)
                    .WithLocation(0)
                    .WithArguments("Before", "BaseClass", "SetupAsync")
            );
    }

    [Test]
    public async Task After_Test_On_Both_Base_And_Override_Reports_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;
                using static TUnit.Core.HookType;

                public class BaseClass
                {
                    [After(Test)]
                    public virtual Task TeardownAsync() => Task.CompletedTask;
                }

                public class DerivedClass : BaseClass
                {
                    [{|#0:After(Test)|}]
                    public override Task TeardownAsync() => base.TeardownAsync();
                }
                """,
                Verifier.Diagnostic(Rules.RedundantHookAttributeOnOverride)
                    .WithLocation(0)
                    .WithArguments("After", "BaseClass", "TeardownAsync")
            );
    }

    [Test]
    public async Task Attribute_Only_On_Base_Is_Fine()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;
                using static TUnit.Core.HookType;

                public class BaseClass
                {
                    [Before(Test)]
                    public virtual Task SetupAsync() => Task.CompletedTask;
                }

                public class DerivedClass : BaseClass
                {
                    public override Task SetupAsync() => base.SetupAsync();
                }
                """
            );
    }

    [Test]
    public async Task Attribute_Only_On_Override_Is_Fine()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;
                using static TUnit.Core.HookType;

                public class BaseClass
                {
                    public virtual Task SetupAsync() => Task.CompletedTask;
                }

                public class DerivedClass : BaseClass
                {
                    [Before(Test)]
                    public override Task SetupAsync() => base.SetupAsync();
                }
                """
            );
    }

    [Test]
    public async Task Abstract_Intermediate_With_InheritsTests_Reports_Error_On_Intermediate()
    {
        // Regression shape for https://github.com/thomhurst/TUnit/issues/5450 — an abstract
        // intermediate overrides the base hook and then concrete subclasses pick up the tests via
        // [InheritsTests]. The duplication happens on the intermediate's override, so that's where
        // the diagnostic should fire.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;
                using static TUnit.Core.HookType;

                public class BaseClass
                {
                    [Before(Test)]
                    public virtual Task SetupAsync() => Task.CompletedTask;
                }

                public abstract class IntermediateClass : BaseClass
                {
                    [{|#0:Before(Test)|}]
                    public override Task SetupAsync() => base.SetupAsync();

                    [Test]
                    public void DoTest() { }
                }

                [InheritsTests]
                public class ConcreteA : IntermediateClass;

                [InheritsTests]
                public class ConcreteB : IntermediateClass;
                """,
                Verifier.Diagnostic(Rules.RedundantHookAttributeOnOverride)
                    .WithLocation(0)
                    .WithArguments("Before", "BaseClass", "SetupAsync")
            );
    }

    [Test]
    public async Task Chain_With_Gap_Still_Reports_Error_Via_Transitive_Ancestor()
    {
        // A has the hook attribute, B overrides without it (fine — virtual dispatch), C overrides
        // with the hook attribute again — C is the source of the duplication because A is still
        // in the ancestor chain.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;
                using static TUnit.Core.HookType;

                public class A
                {
                    [Before(Test)]
                    public virtual Task SetupAsync() => Task.CompletedTask;
                }

                public class B : A
                {
                    public override Task SetupAsync() => base.SetupAsync();
                }

                public class C : B
                {
                    [{|#0:Before(Test)|}]
                    public override Task SetupAsync() => base.SetupAsync();
                }
                """,
                Verifier.Diagnostic(Rules.RedundantHookAttributeOnOverride)
                    .WithLocation(0)
                    .WithArguments("Before", "A", "SetupAsync")
            );
    }

    [Test]
    public async Task Mismatched_Hook_Types_Are_Fine()
    {
        // [Before(Test)] on the base and [After(Test)] on the override are different hooks; both
        // run in their own phase, so there is no duplication.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;
                using static TUnit.Core.HookType;

                public class BaseClass
                {
                    [Before(Test)]
                    public virtual Task SetupAsync() => Task.CompletedTask;
                }

                public class DerivedClass : BaseClass
                {
                    [After(Test)]
                    public override Task SetupAsync() => base.SetupAsync();
                }
                """
            );
    }

    [Test]
    public async Task New_Method_Is_Fine()
    {
        // `new` is not `override` — derived's method is a different method, so both hooks are
        // registered on different targets and neither double-fires.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;
                using static TUnit.Core.HookType;

                public class BaseClass
                {
                    [Before(Test)]
                    public virtual Task SetupAsync() => Task.CompletedTask;
                }

                public class DerivedClass : BaseClass
                {
                    [Before(Test)]
                    public new Task SetupAsync() => Task.CompletedTask;
                }
                """
            );
    }

}
