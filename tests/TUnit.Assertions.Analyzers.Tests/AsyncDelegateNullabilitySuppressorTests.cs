using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace TUnit.Assertions.Analyzers.Tests;

/// <summary>
/// Tests for the AsyncDelegateNullabilitySuppressor, which suppresses CS8619 and CS8621 when a lambda or method group
/// returning <c>Task&lt;T&gt;</c> is passed to an <c>Assert.That</c> overload declared with a <c>Func&lt;Task&lt;...&gt;&gt;</c>.
/// </summary>
public class AsyncDelegateNullabilitySuppressorTests
{
    private static readonly DiagnosticResult CS8619 = new("CS8619", DiagnosticSeverity.Warning);
    private static readonly DiagnosticResult CS8621 = new("CS8621", DiagnosticSeverity.Warning);

    [Test]
    [Arguments("await Assert.That(() => {|#0:GetNameAsync()|}).IsEqualTo(\"Alice\");")]
    [Arguments("await Assert.That(() => ({|#0:GetNameAsync()|})).IsEqualTo(\"Alice\");")]
    [Arguments("await Assert.That((() => {|#0:GetNameAsync()|})).IsEqualTo(\"Alice\");")]
    [Arguments("await Assert.That(() => { return {|#0:GetNameAsync()|}; }).IsEqualTo(\"Alice\");")]
    [Arguments("await Assert.That(() => { if (Flag) { return {|#0:GetNameAsync()|}; } return Task.FromResult<string?>(null); }).IsNotNull();")]
    [Arguments("await Assert.That(delegate { return {|#0:GetNameAsync()|}; }).IsEqualTo(\"Alice\");")]
    [Arguments("await Assert.That(func: () => {|#0:GetNameAsync()|}).IsEqualTo(\"Alice\");")]
    [Arguments("await Assert.That<string>(() => {|#0:GetNameAsync()|}).IsEqualTo(\"Alice\");")]
    [Arguments("await Assert.That(() => {|#0:GetNumbersAsync()|}).Contains(1);")]
    public async Task Suppresses_A_Lambda_Returning_A_Non_Nullable_Task_Result(string statement)
    {
        await AnalyzerTestHelpers
            .CreateSuppressorTest<AsyncDelegateNullabilitySuppressor>(Code(statement))
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8619)
            .WithExpectedDiagnosticsResults(CS8619.WithLocation(0).WithIsSuppressed(true))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    [Arguments("await Assert.That({|#0:GetNameAsync|}).IsEqualTo(\"Alice\");")]
    [Arguments("await Assert.That<string>({|#0:GetNameAsync|}).IsEqualTo(\"Alice\");")]
    [Arguments("await Assert.That(({|#0:GetNameAsync|})).IsEqualTo(\"Alice\");")]
    public async Task Suppresses_A_Method_Group_Returning_A_Non_Nullable_Task_Result(string statement)
    {
        await AnalyzerTestHelpers
            .CreateSuppressorTest<AsyncDelegateNullabilitySuppressor>(Code(statement))
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8621)
            .WithExpectedDiagnosticsResults(CS8621.WithLocation(0).WithIsSuppressed(true))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    // Not an argument of Assert.That.
    [Arguments("Func<Task<string?>> name = () => {|#0:GetNameAsync()|}; await name();")]
    // Another method named That.
    [Arguments("await Other.That(() => {|#0:GetNameAsync()|});")]
    // The Func<TValue?> overload with a task as TValue: the assertion is on the task, not its result.
    [Arguments("await Assert.That<Task<string?>>(() => {|#0:GetNameAsync()|}).IsNotNull();")]
    // List<string> for List<string?>?: the element annotation differs too, which this suppressor leaves alone.
    [Arguments("await Assert.That<List<string?>>(() => {|#0:GetNamesAsync()|}).IsNotNull();")]
    public async Task Does_Not_Suppress_Other_CS8619(string statement)
    {
        await AnalyzerTestHelpers
            .CreateSuppressorTest<AsyncDelegateNullabilitySuppressor>(Code(statement))
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8619)
            .WithExpectedDiagnosticsResults(CS8619.WithLocation(0).WithIsSuppressed(false))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    // Not an argument of Assert.That.
    [Arguments("Func<Task<string?>> name = {|#0:GetNameAsync|}; await name();")]
    // A task that may itself be null: the assertion would await null.
    [Arguments("await Assert.That<string>({|#0:GetMaybeTaskAsync|}).IsNotNull();")]
    public async Task Does_Not_Suppress_Other_CS8621(string statement)
    {
        await AnalyzerTestHelpers
            .CreateSuppressorTest<AsyncDelegateNullabilitySuppressor>(Code(statement))
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8621)
            .WithExpectedDiagnosticsResults(CS8621.WithLocation(0).WithIsSuppressed(false))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    // The conditional and the switch expression take the type of their first branch, Task<string>: the warning on the
    // whole expression (to the lambda's Task<string?>) is suppressed, the one on the other branch (Task<string?> to
    // Task<string>) is a real mismatch and stays.
    [Arguments("await Assert.That(() => {|#0:Flag ? GetNameAsync() : {|#1:GetMaybeNameAsync()|}|}).IsNotNull();")]
    [Arguments("await Assert.That(() => {|#0:Flag switch { true => GetNameAsync(), _ => {|#1:GetMaybeNameAsync()|} }|}).IsNotNull();")]
    public async Task Suppresses_Only_The_Conversion_To_The_Lambda_Result_In_A_Conditional(string statement)
    {
        await AnalyzerTestHelpers
            .CreateSuppressorTest<AsyncDelegateNullabilitySuppressor>(Code(statement))
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8619)
            .WithExpectedDiagnosticsResults(
                CS8619.WithLocation(0).WithIsSuppressed(true),
                CS8619.WithLocation(1).WithIsSuppressed(false))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task An_Async_Lambda_Does_Not_Warn_In_The_First_Place()
    {
        // The compiler target-types the async lambda's task, so there is nothing to suppress.
        await AnalyzerTestHelpers
            .CreateSuppressorTest<AsyncDelegateNullabilitySuppressor>(Code("await Assert.That(async () => await GetNameAsync()).IsEqualTo(\"Alice\");"))
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8619, CS8621)
            .WithExpectedDiagnosticsResults()
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Does_Not_Suppress_For_A_Source_Defined_TUnit_Lookalike()
    {
        var code = """
            #nullable enable
            using System;
            using System.Threading.Tasks;

            namespace TUnit.Assertions
            {
                public static class Assert
                {
                    public static Task That<T>(Func<Task<T?>> func) => Task.CompletedTask;
                }
            }

            public class MyTests
            {
                private static Task<string> GetNameAsync() => Task.FromResult("Alice");

                public async Task TestMethod()
                {
                    await TUnit.Assertions.Assert.That(() => {|#0:GetNameAsync()|});
                }
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<AsyncDelegateNullabilitySuppressor>(code)
            .IgnoringDiagnostics("CS1591", "CS0436")
            .WithSpecificDiagnostics(CS8619)
            .WithExpectedDiagnosticsResults(CS8619.WithLocation(0).WithIsSuppressed(false))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    private static string Code(string statement) => $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using TUnit.Assertions;
        using TUnit.Assertions.Extensions;

        public static class Other
        {
            public static Task That<T>(Func<Task<T?>> func) => Task.CompletedTask;
        }

        public class MyTests
        {
            private static bool Flag => DateTime.Now.Ticks > 0;
            private static Task<string> GetNameAsync() => Task.FromResult("Alice");
            private static Task<string?> GetMaybeNameAsync() => Task.FromResult<string?>(null);
            private static Task<string?>? GetMaybeTaskAsync() => null;
            private static Task<IEnumerable<int>> GetNumbersAsync() => Task.FromResult<IEnumerable<int>>(new[] { 1 });
            private static Task<List<string>> GetNamesAsync() => Task.FromResult(new List<string> { "Alice" });

            public async Task TestMethod()
            {
                {{statement}}
            }
        }
        """;
}
