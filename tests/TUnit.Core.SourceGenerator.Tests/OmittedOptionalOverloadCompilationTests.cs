using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/7122.
///
/// When a row omitted optional parameters, the generated call left them out, so a sibling overload
/// without those parameters could win overload resolution (or make the call ambiguous). The generated
/// call now passes each omitted parameter's declared default explicitly. This compiles the generated
/// output and checks both the emitted calls and that every kind of default compiles.
/// </summary>
internal class OmittedOptionalOverloadCompilationTests
{
    private const string Source =
        """
        #nullable enable
        global using global::System;
        global using global::System.Threading;
        global using global::System.Threading.Tasks;
        global using global::TUnit.Core;

        namespace MyTests;

        public enum Mode { First = 1, Second = 2 }

        [Flags]
        public enum Flags : long { None = 0, A = 1, B = 1L << 40 }

        public class OverloadTests
        {
            [Test]
            [Arguments]
            public void Before_Params(int count = 42, params int[] values) { }

            internal void Before_Params(params int[] values) { }

            [Test]
            [Arguments]
            public void Before_Params_With_Token(int count = 42, CancellationToken token = default, params int[] values) { }

            internal void Before_Params_With_Token(CancellationToken token = default, params int[] values) { }

            [Test]
            [Arguments(1)]
            public void Trailing(int first, int second = 42) { }

            internal void Trailing(int first) { }

            [Test]
            [Arguments]
            public void Every_Kind(
                Mode mode = Mode.Second,
                Flags flags = Flags.A | Flags.B,
                decimal money = -1.5m,
                double ratio = 1e300,
                float single = 0.1f,
                float nan = float.NaN,
                double negativeInfinity = double.NegativeInfinity,
                float positiveInfinity = float.PositiveInfinity,
                long min = long.MinValue,
                int intMin = int.MinValue,
                double large = 1E+20,
                char nul = '\0',
                ulong max = ulong.MaxValue,
                byte small = 7,
                sbyte negative = -8,
                char quote = '\'',
                bool flag = true,
                string text = "a \"quoted\"\n\\ text",
                string? nullable = null,
                string notNullable = null!,
                int? nullableValue = 3,
                int? nullableNull = null,
                TimeSpan structDefault = default,
                Guid newStruct = new Guid(),
                Mode? nullableEnum = Mode.First,
                params int[] values) { }

            internal void Every_Kind(params int[] values) { }

            [Test]
            [GenerateGenericTest(typeof(int))]
            [Arguments]
            public void Generic_Default<T>(int count = 42, T value = default!, params T[] values) { }

            internal void Generic_Default<T>(params T[] values) { }

            // A string default for a broader declared type (only expressible through DefaultParameterValue)
            // must keep that type, or the sibling wins.
            [Test]
            [Arguments]
            public void Object_String_Default([global::System.Runtime.InteropServices.Optional, global::System.Runtime.InteropServices.DefaultParameterValue("declared")] object value) { }

            internal void Object_String_Default(string value) { }

            // Non-finite defaults keep their own type when boxed into a broader declared type.
            [Test]
            [Arguments]
            public void Object_Non_Finite([global::System.Runtime.InteropServices.Optional, global::System.Runtime.InteropServices.DefaultParameterValue(float.NaN)] object floatNan, [global::System.Runtime.InteropServices.Optional, global::System.Runtime.InteropServices.DefaultParameterValue(float.NegativeInfinity)] object floatInfinity, [global::System.Runtime.InteropServices.Optional, global::System.Runtime.InteropServices.DefaultParameterValue(double.NaN)] object doubleNan) { }

            // Integer defaults of a type other than int keep that type when boxed into a broader declared type.
            [Test]
            [Arguments]
            public void Object_Integral_Defaults([global::System.Runtime.InteropServices.Optional, global::System.Runtime.InteropServices.DefaultParameterValue((byte)7)] object b, [global::System.Runtime.InteropServices.Optional, global::System.Runtime.InteropServices.DefaultParameterValue((short)-3)] object s, [global::System.Runtime.InteropServices.Optional, global::System.Runtime.InteropServices.DefaultParameterValue(5L)] object l, [global::System.Runtime.InteropServices.Optional, global::System.Runtime.InteropServices.DefaultParameterValue(9UL)] object ul) { }

            // A default that cannot be written as a C# literal falls back to omitting the parameter.
            [Test]
            [Arguments]
            public void Unwritable_Default([global::System.Runtime.InteropServices.Optional, global::System.Runtime.CompilerServices.DateTimeConstant(0)] DateTime when, params int[] values) { }
        }
        """;

    private const string DynamicSource =
        """
        #nullable enable
        global using global::System;
        global using global::System.Threading.Tasks;
        global using global::TUnit.Core;

        namespace MyTests;

        public class DynamicTests
        {
            [Test]
            [Arguments]
            public void Dynamic_String_Default([global::System.Runtime.InteropServices.Optional, global::System.Runtime.InteropServices.DefaultParameterValue("declared")] dynamic value) { }

            internal void Dynamic_String_Default(string value) { }

            [Test]
            [Arguments]
            public void Dynamic_Null_Default(dynamic? value = null) { }

            internal void Dynamic_Null_Default(string value) { }
        }
        """;

    [Test]
    public async Task Omitted_optionals_pass_declared_defaults_explicitly()
    {
        var (generated, problems) = Generate(Source);

        // Before the fix, Before_Params_With_Token failed with CS0121 (ambiguous call).
        await Assert.That(problems)
            .IsEmpty()
            .Because($"generated code must compile. Errors:{Environment.NewLine}"
                + string.Join(Environment.NewLine, problems.Select(e => e.ToString()))
                + Environment.NewLine + generated);

        await Assert.That(generated).Contains("instance.Before_Params((int)(42), global::System.Array.Empty<int>())");
        await Assert.That(generated).Contains("instance.Before_Params_With_Token((int)(42), ");
        await Assert.That(generated).Contains("instance.Trailing(global::TUnit.Core.Helpers.CastHelper.Cast<int>(args[0]), (int)(42))");
        await Assert.That(generated).Contains("instance.Every_Kind((global::MyTests.Mode)(2), ");
        await Assert.That(generated).Contains("instance.Generic_Default<int>((int)(42), default(int)!, global::System.Array.Empty<int>())");
        await Assert.That(generated).Contains("instance.Object_String_Default((object)(\"declared\"))");
        await Assert.That(generated).Contains("instance.Object_Non_Finite((object)(float.NaN), (object)(float.NegativeInfinity), (object)(double.NaN))");
        await Assert.That(generated).Contains("instance.Object_Integral_Defaults((object)((byte)(7)), (object)((short)(-3)), (object)((long)(5)), (object)((ulong)(9)))");
        await Assert.That(generated).Contains("instance.Unwritable_Default(@values: global::System.Array.Empty<int>())");
    }

    [Test]
    public async Task Dynamic_defaults_are_passed_as_object()
    {
        // A `dynamic` argument would make the call late-bound, so the runtime binder could pick the
        // sibling overload. The default is passed as `object` instead, keeping the call bound to the test.
        var (generated, problems) = Generate(DynamicSource);

        // dynamic test parameters do not compile yet for an unrelated reason: the parameter metadata
        // uses typeof(dynamic) (https://github.com/thomhurst/TUnit/issues/7165). No other error is allowed.
        var otherProblems = problems.Where(d => d.Id != "CS1962").ToArray();
        await Assert.That(otherProblems)
            .IsEmpty()
            .Because(string.Join(Environment.NewLine, otherProblems.Select(e => e.ToString())));

        await Assert.That(generated).Contains("instance.Dynamic_String_Default((object)(\"declared\"))");
        await Assert.That(generated).Contains("instance.Dynamic_Null_Default(default(object)!)");
        await Assert.That(generated).DoesNotContain("(dynamic)(");
        await Assert.That(generated).DoesNotContain("default(dynamic)");
    }

    private static (string Generated, Diagnostic[] Problems) Generate(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);

        var compilation = CSharpCompilation.Create(
                "OmittedOptionalOverloads",
                [CSharpSyntaxTree.ParseText(source, parseOptions)],
                ReferencesHelper.References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMetadataGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        var generatedTrees = output.SyntaxTrees.Skip(1).ToArray();
        var generated = string.Join(
            Environment.NewLine + "----" + Environment.NewLine,
            generatedTrees.Select(t => t.GetText().ToString()));

        var problems = output.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        return (generated, problems);
    }
}
