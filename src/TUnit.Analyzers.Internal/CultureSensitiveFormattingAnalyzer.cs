using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace TUnit.Analyzers.Internal;

/// <summary>
/// Flags text conversions of numbers and dates that silently use the build machine's culture.
/// TUnit's generators, analyzers and code fixers run inside every consumer's compiler: under
/// sv-SE, fi-FI or nb-NO a negative number formats with U+2212 MINUS SIGN, and under de-DE or
/// fr-FR a double formats with a decimal comma — both produce generated C# that does not compile,
/// and parsing with the wrong separator silently changes values.
/// CA1305 only sees explicit calls such as <c>ToString()</c> and <c>string.Format</c>; it misses
/// interpolated strings, <c>+</c> concatenation and <c>StringBuilder.Append(int)</c>, which is
/// where generated code is built. This rule covers all of them.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CultureSensitiveFormattingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TUNITINT001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Culture-sensitive number or date conversion",
        messageFormat: "{0} converts '{1}' using the current culture; use CultureInfo.InvariantCulture (e.g. value.ToString(CultureInfo.InvariantCulture) or FormattableString.Invariant)",
        category: "Globalization",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Build-time code must behave identically under every culture. Format and parse numbers and dates with CultureInfo.InvariantCulture.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            var types = new KnownTypes(start.Compilation);

            start.RegisterOperationAction(c => AnalyzeInterpolation(c, types), OperationKind.Interpolation);
            start.RegisterOperationAction(c => AnalyzeHandlerAppend(c, types), OperationKind.InterpolatedStringAppendFormatted);
            start.RegisterOperationAction(c => AnalyzeConcatenation(c, types), OperationKind.Binary);
            start.RegisterOperationAction(c => AnalyzeInvocation(c, types), OperationKind.Invocation);
        });
    }

    private static void AnalyzeInterpolation(OperationAnalysisContext context, KnownTypes types)
    {
        var interpolation = (IInterpolationOperation) context.Operation;
        var valueType = UnderlyingType(interpolation.Expression);

        if (!types.IsCultureSensitive(valueType) || IsHexFormat(interpolation.FormatString))
        {
            return;
        }

        if (interpolation.Parent is not IInterpolatedStringOperation interpolatedString
            || FormatsWithExplicitCulture(interpolatedString, types))
        {
            return;
        }

        Report(context, interpolation.Expression, "Interpolation", valueType!);
    }

    /// <summary>
    /// <c>sb.Append($"{i}")</c> binds to an interpolated string handler, whose holes are
    /// <c>AppendFormatted</c> calls rather than plain interpolations.
    /// </summary>
    private static void AnalyzeHandlerAppend(OperationAnalysisContext context, KnownTypes types)
    {
        var append = (IInterpolatedStringAppendOperation) context.Operation;

        if (append.AppendCall is not IInvocationOperation { Arguments.Length: > 0 } call)
        {
            return;
        }

        var value = call.Arguments[0].Value;
        var valueType = UnderlyingType(value);
        var format = call.Arguments.FirstOrDefault(a => a.Parameter?.Name == "format")?.Value;

        if (!types.IsCultureSensitive(valueType) || IsHexFormat(format))
        {
            return;
        }

        if (append.Parent is IInterpolatedStringOperation interpolatedString
            && FormatsWithExplicitCulture(interpolatedString, types))
        {
            return;
        }

        Report(context, value, "Interpolation", valueType!);
    }

    private static void AnalyzeConcatenation(OperationAnalysisContext context, KnownTypes types)
    {
        var binary = (IBinaryOperation) context.Operation;

        if (binary.OperatorKind != BinaryOperatorKind.Add || binary.Type?.SpecialType != SpecialType.System_String)
        {
            return;
        }

        foreach (var operand in new[] { binary.LeftOperand, binary.RightOperand })
        {
            var operandType = UnderlyingType(operand);

            if (types.IsCultureSensitive(operandType))
            {
                Report(context, operand, "String concatenation", operandType!);
            }
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, KnownTypes types)
    {
        var invocation = (IInvocationOperation) context.Operation;
        var method = invocation.TargetMethod;

        if (HasFormatProviderParameter(method, types))
        {
            return;
        }

        // value.ToString() / value.ToString("N2")
        if (method.Name == nameof(ToString) && invocation.Instance is { } instance)
        {
            var instanceType = UnderlyingType(instance);

            if (types.IsCultureSensitive(instanceType)
                && !IsHexFormat(invocation.Arguments.FirstOrDefault()?.Value.ConstantValue is { HasValue: true, Value: string format } ? format : null))
            {
                Report(context, invocation, "ToString()", instanceType!);
            }

            return;
        }

        // int.Parse(text), double.TryParse(text, out var d)
        if (method.Name is "Parse" or "TryParse" && method.IsStatic && types.IsCultureSensitive(method.ContainingType))
        {
            Report(context, invocation, $"{method.ContainingType.Name}.{method.Name}", method.ContainingType);
            return;
        }

        // string.Format("{0}", value)
        if (method.Name == "Format" && method.ContainingType.SpecialType == SpecialType.System_String)
        {
            foreach (var argument in invocation.Arguments.Skip(1))
            {
                ReportArgumentIfCultureSensitive(context, types, argument, "string.Format");
            }

            return;
        }

        // Convert.ToString(value) formats; Convert.ToDouble(text or boxed value) may parse.
        if (SymbolEqualityComparer.Default.Equals(method.ContainingType, types.Convert)
            && invocation.Arguments.FirstOrDefault() is { } converted)
        {
            if (method.Name == "ToString")
            {
                ReportArgumentIfCultureSensitive(context, types, converted, "Convert.ToString");
            }
            else if (types.IsCultureSensitive(method.ReturnType)
                     && UnderlyingType(converted.Value) is { SpecialType: SpecialType.System_String or SpecialType.System_Object })
            {
                Report(context, invocation, $"Convert.{method.Name}", method.ReturnType);
            }

            return;
        }

        // StringBuilder.Append(int), TextWriter.Write(double), and their Insert/AppendLine/WriteLine kin.
        // Only the formatted arguments count: Append(char, repeatCount) and Insert(index, ...) take
        // integers that are never turned into text.
        if (IsTextSink(method, types))
        {
            foreach (var argument in invocation.Arguments)
            {
                if (argument.Parameter is { Name: var name } && (name == "value" || name.StartsWith("arg", StringComparison.Ordinal)))
                {
                    ReportArgumentIfCultureSensitive(context, types, argument, $"{method.ContainingType.Name}.{method.Name}");
                }
            }
        }
    }

    private static void ReportArgumentIfCultureSensitive(OperationAnalysisContext context, KnownTypes types, IArgumentOperation argument, string conversion)
    {
        var argumentType = UnderlyingType(argument.Value);

        if (types.IsCultureSensitive(argumentType))
        {
            Report(context, argument.Value, conversion, argumentType!);
        }
    }

    /// <summary>
    /// An interpolated string formats with an explicit culture when it is converted to
    /// <see cref="FormattableString"/>/<see cref="IFormattable"/> (the consumer, e.g.
    /// <c>FormattableString.Invariant</c>, picks the culture) or built by a handler that was
    /// given an <see cref="IFormatProvider"/> (<c>string.Create(provider, $"...")</c>).
    /// </summary>
    private static bool FormatsWithExplicitCulture(IInterpolatedStringOperation interpolatedString, KnownTypes types)
    {
        var parent = interpolatedString.Parent;

        if (parent is IConversionOperation conversion
            && (SymbolEqualityComparer.Default.Equals(conversion.Type, types.FormattableString)
                || SymbolEqualityComparer.Default.Equals(conversion.Type, types.Formattable)))
        {
            return true;
        }

        if (parent is IInterpolatedStringHandlerCreationOperation { HandlerCreation: IObjectCreationOperation creation })
        {
            return creation.Arguments.Any(a =>
                types.IsFormatProvider(a.Parameter?.Type) && a.Value.ConstantValue is not { HasValue: true, Value: null });
        }

        return false;
    }

    private static bool HasFormatProviderParameter(IMethodSymbol method, KnownTypes types)
        => method.Parameters.Any(p => types.IsFormatProvider(p.Type));

    private static bool IsTextSink(IMethodSymbol method, KnownTypes types)
    {
        if (method.Name is not ("Append" or "AppendLine" or "Insert" or "Write" or "WriteLine"))
        {
            return false;
        }

        for (var type = method.ContainingType; type is not null; type = type.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(type, types.StringBuilder)
                || SymbolEqualityComparer.Default.Equals(type, types.TextWriter))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsHexFormat(IOperation? format)
        => IsHexFormat(format?.ConstantValue is { HasValue: true, Value: string text } ? text : null);

    private static bool IsHexFormat(string? format)
        => format is { Length: > 0 } && (format[0] == 'x' || format[0] == 'X');

    /// <summary>The operand's type before any implicit boxing or widening, unwrapping Nullable&lt;T&gt;.</summary>
    private static ITypeSymbol? UnderlyingType(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
        {
            operation = conversion.Operand;
        }

        var type = operation.Type;

        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            type = nullable.TypeArguments[0];
        }

        return type;
    }

    private static void Report(OperationAnalysisContext context, IOperation operation, string conversion, ITypeSymbol type)
        => context.ReportDiagnostic(Diagnostic.Create(Rule, operation.Syntax.GetLocation(), conversion, type.ToDisplayString()));

    private sealed class KnownTypes
    {
        private readonly ImmutableHashSet<ITypeSymbol> _otherCultureSensitive;

        public KnownTypes(Compilation compilation)
        {
            FormatProvider = compilation.GetTypeByMetadataName("System.IFormatProvider");
            FormattableString = compilation.GetTypeByMetadataName("System.FormattableString");
            Formattable = compilation.GetTypeByMetadataName("System.IFormattable");
            StringBuilder = compilation.GetTypeByMetadataName("System.Text.StringBuilder");
            TextWriter = compilation.GetTypeByMetadataName("System.IO.TextWriter");
            Convert = compilation.GetTypeByMetadataName("System.Convert");

            _otherCultureSensitive = new[]
                {
                    "System.DateTimeOffset",
                    "System.Numerics.BigInteger",
                    "System.Half",
                }
                .Select(compilation.GetTypeByMetadataName)
                .Where(t => t is not null)
                .Cast<ITypeSymbol>()
                .ToImmutableHashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        }

        public INamedTypeSymbol? FormatProvider { get; }
        public INamedTypeSymbol? FormattableString { get; }
        public INamedTypeSymbol? Formattable { get; }
        public INamedTypeSymbol? StringBuilder { get; }
        public INamedTypeSymbol? TextWriter { get; }
        public INamedTypeSymbol? Convert { get; }

        public bool IsFormatProvider(ITypeSymbol? type)
            => type is not null && SymbolEqualityComparer.Default.Equals(type, FormatProvider);

        /// <summary>
        /// Signed integers (the negative sign varies), floating point and decimal (the decimal
        /// separator varies) and dates. Unsigned integers, bool, char and enums format the same
        /// in every culture.
        /// </summary>
        public bool IsCultureSensitive(ITypeSymbol? type) => type is not null && (type.SpecialType switch
        {
            SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64
                or SpecialType.System_IntPtr or SpecialType.System_Single or SpecialType.System_Double
                or SpecialType.System_Decimal or SpecialType.System_DateTime => true,
            _ => _otherCultureSensitive.Contains(type),
        });
    }
}
