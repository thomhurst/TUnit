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
    [Arguments("await Assert.That(static () => {|#0:GetNameAsync()|}).IsEqualTo(\"Alice\");")]
    [Arguments("await That(() => {|#0:GetNameAsync()|}).IsEqualTo(\"Alice\");")]
    [Arguments("await global::TUnit.Assertions.Assert.That(() => {|#0:GetNameAsync()|}).IsEqualTo(\"Alice\");")]
    // Only the top-level annotation differs: List<string?> for List<string?>?.
    [Arguments("await Assert.That(() => {|#0:GetMaybeNamesAsync()|}).IsNotNull();")]
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
    [Arguments("await Assert.That({|#0:this.GetInstanceNameAsync|}).IsEqualTo(\"Alice\");")]
    [Arguments("await Assert.That({|#0:GetNewAsync<List<int>>|}).IsNotNull();")]
    [Arguments("await Assert.That({|#0:GetNumbersAsync|}).Contains(1);")]
    // The warning sits on the head of a lambda with an explicit return type.
    [Arguments("await Assert.That({|#0:Task<string> () =>|} GetNameAsync()).IsEqualTo(\"Alice\");")]
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
    // A return of a local function or a nested lambda inside the asserted lambda is not the asserted lambda's result.
    [Arguments("await Assert.That(() => { Task<string?> Local() { return {|#0:GetNameAsync()|}; } return Local(); }).IsNotNull();")]
    [Arguments("await Assert.That(() => { Func<Task<string?>> inner = () => {|#0:GetNameAsync()|}; return inner(); }).IsNotNull();")]
    // List<string> for List<string?>?: the element annotation differs too, which this suppressor leaves alone.
    [Arguments("await Assert.That<List<string?>>(() => {|#0:GetNamesAsync()|}).IsNotNull();")]
    [Arguments("await Assert.That(() => ({|#0:GetMaybeNameAsync() as Task<string>|})).IsNotNull();")]
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
    // [return: MaybeNull]: the task itself may be null, which its type does not show.
    [Arguments("await Assert.That({|#0:GetMaybeNullTaskAsync|}).IsNotNull();")]
    [Arguments("await Assert.That({|#0:GetMaybeNullValue<Task<string>>|}).IsNotNull();")]
    [Arguments("await Assert.That({|#0:[return: MaybeNull] Task<string> () =>|} GetNameAsync()).IsNotNull();")]
    // A cast of the method group: the warning belongs to the cast.
    [Arguments("await Assert.That({|#0:(Func<Task<string?>>)GetNameAsync|}).IsEqualTo(\"Alice\");")]
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
    // The conditional and the switch expression take the non-nullable type Task<string>: the warning on the whole
    // expression (to the lambda's Task<string?>) is suppressed, the one on the Task<string?> branch is a real mismatch
    // and stays.
    [Arguments("await Assert.That(() => {|#0:Flag ? GetNameAsync() : {|#1:GetMaybeNameAsync()|}|}).IsNotNull();")]
    [Arguments("await Assert.That(() => {|#0:Flag ? {|#1:GetMaybeNameAsync()|} : GetNameAsync()|}).IsNotNull();")]
    // Another warning in the same lambda stays.
    [Arguments("await Assert.That(() => { List<string?> names = {|#1:new List<string>()|}; return {|#0:GetNameAsync()|}; }).IsNotNull();")]
    [Arguments("await Assert.That(() => {|#0:Flag switch { true => GetNameAsync(), _ => {|#1:GetMaybeNameAsync()|} }|}).IsNotNull();")]
    public async Task Suppresses_Only_The_Conversion_To_The_Lambda_Result(string statement)
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
    public async Task A_Typed_Lambda_Keeps_The_Warning_On_Its_Own_Body()
    {
        // The body's Task<string?> does not fit the lambda's declared Task<string>: a real mismatch, which stays. The
        // lambda's Task<string> going to the delegate's Task<string?> is the false positive.
        await AnalyzerTestHelpers
            .CreateSuppressorTest<AsyncDelegateNullabilitySuppressor>(Code("await Assert.That({|#1:Task<string> () =>|} {|#0:GetMaybeNameAsync()|}).IsNotNull();"))
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8619, CS8621)
            .WithExpectedDiagnosticsResults(
                CS8619.WithLocation(0).WithIsSuppressed(false),
                CS8621.WithLocation(1).WithIsSuppressed(true))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Does_Not_Suppress_For_A_Task_That_May_Itself_Be_Null()
    {
        // The compiler reports the possibly null task as CS8603 next to the CS8619; neither is the false positive.
        await AnalyzerTestHelpers
            .CreateSuppressorTest<AsyncDelegateNullabilitySuppressor>(Code("await Assert.That(() => {|#0:GetNullableTaskAsync()|}).IsNotNull();"))
            .IgnoringDiagnostics("CS1591", "CS8603")
            .WithSpecificDiagnostics(CS8619)
            .WithExpectedDiagnosticsResults(CS8619.WithLocation(0).WithIsSuppressed(false))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Does_Not_Suppress_A_Cast_Result()
    {
        // The cast's own conversion (Task<string?> to Task<string>) is a real mismatch, and its warning has the same span
        // as the one on the lambda's result, so neither is suppressed.
        await AnalyzerTestHelpers
            .CreateSuppressorTest<AsyncDelegateNullabilitySuppressor>(Code("await Assert.That(() => {|#0:(Task<string>)GetMaybeNameAsync()|}).IsNotNull();"))
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8619)
            .WithExpectedDiagnosticsResults(
                CS8619.WithLocation(0).WithIsSuppressed(false),
                CS8619.WithLocation(0).WithIsSuppressed(false))
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
        using System.Diagnostics.CodeAnalysis;
        using System.Threading.Tasks;
        using TUnit.Assertions;
        using TUnit.Assertions.Extensions;
        using static TUnit.Assertions.Assert;

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
            private static Task<string>? GetNullableTaskAsync() => null;
            [return: MaybeNull] private static Task<string> GetMaybeNullTaskAsync() => null!;
            [return: MaybeNull] private static T GetMaybeNullValue<T>() => default!;
            private static Task<List<string?>> GetMaybeNamesAsync() => Task.FromResult(new List<string?> { null });
            private static Task<T> GetNewAsync<T>() where T : new() => Task.FromResult(new T());
            private Task<string> GetInstanceNameAsync() => Task.FromResult("Alice");
            private static Task<IEnumerable<int>> GetNumbersAsync() => Task.FromResult<IEnumerable<int>>(new[] { 1 });
            private static Task<List<string>> GetNamesAsync() => Task.FromResult(new List<string> { "Alice" });

            public async Task TestMethod()
            {
                {{statement}}
            }
        }
        """;
}
