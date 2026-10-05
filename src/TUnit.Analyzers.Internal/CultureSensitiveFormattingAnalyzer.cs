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
/// interpolated strings, <c>+</c> / <c>+=</c> concatenation and <c>StringBuilder.Append(int)</c>,
/// which is where generated code is built. This rule covers all of them.
/// <para>
/// Roslyn's <c>TypedConstant.Value</c> is treated as a possible number: it is how generators read
/// user attribute arguments such as <c>[Arguments(-1.5)]</c>, typed only as <c>object</c>.
/// </para>
/// <para>
/// Limit: other values typed <c>object</c> or an unconstrained generic (e.g.
/// <c>$"{(object) i}"</c>) are not seen.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CultureSensitiveFormattingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TUNITINT001";

    private const string TypedConstantValueDisplay = "TypedConstant.Value (may hold a number)";

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
            start.RegisterOperationAction(c => AnalyzeCompoundConcatenation(c, types), OperationKind.CompoundAssignment);
            start.RegisterOperationAction(c => AnalyzeInvocation(c, types), OperationKind.Invocation);
        });
    }

    private static void AnalyzeInterpolation(OperationAnalysisContext context, KnownTypes types)
    {
        var interpolation = (IInterpolationOperation) context.Operation;

        if (interpolation.Parent is IInterpolatedStringOperation interpolatedString
            && !FormatsWithExplicitCulture(interpolatedString, types))
        {
            ReportIfCultureSensitive(context, types, interpolation.Expression, "Interpolation", interpolation.FormatString);
        }
    }

    /// <summary>
    /// <c>sb.Append($"{i}")</c> binds to an interpolated string handler, whose holes are
    /// <c>AppendFormatted</c> calls rather than plain interpolations.
    /// </summary>
    private static void AnalyzeHandlerAppend(OperationAnalysisContext context, KnownTypes types)
    {
        var append = (IInterpolatedStringAppendOperation) context.Operation;

        if (append.AppendCall is not IInvocationOperation { Arguments.Length: > 0 } call
            || append.Parent is IInterpolatedStringOperation interpolatedString && FormatsWithExplicitCulture(interpolatedString, types))
        {
            return;
        }

        var format = call.Arguments.FirstOrDefault(a => a.Parameter?.Name == "format")?.Value;
        ReportIfCultureSensitive(context, types, call.Arguments[0].Value, "Interpolation", format);
    }

    private static void AnalyzeConcatenation(OperationAnalysisContext context, KnownTypes types)
    {
        var binary = (IBinaryOperation) context.Operation;

        if (binary.OperatorKind == BinaryOperatorKind.Add && binary.Type?.SpecialType == SpecialType.System_String)
        {
            ReportIfCultureSensitive(context, types, binary.LeftOperand, "String concatenation");
            ReportIfCultureSensitive(context, types, binary.RightOperand, "String concatenation");
        }
    }

    // s += i
    private static void AnalyzeCompoundConcatenation(OperationAnalysisContext context, KnownTypes types)
    {
        var assignment = (ICompoundAssignmentOperation) context.Operation;

        if (assignment.OperatorKind == BinaryOperatorKind.Add && assignment.Target.Type?.SpecialType == SpecialType.System_String)
        {
            ReportIfCultureSensitive(context, types, assignment.Value, "String concatenation");
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, KnownTypes types)
    {
        var invocation = (IInvocationOperation) context.Operation;
        var method = invocation.TargetMethod;

        // An IFormatProvider parameter only helps when a provider is actually passed:
        // ToString(null) or an omitted optional provider still means the current culture.
        if (PassesFormatProvider(invocation, types))
        {
            return;
        }

        // value.ToString() / value.ToString("N2") / constant.Value?.ToString()
        if (method.Name == nameof(ToString) && invocation.Instance is { } instance)
        {
            var format = invocation.Arguments.FirstOrDefault(a => a.Value.Type?.SpecialType == SpecialType.System_String)?.Value;
            ReportIfCultureSensitive(context, types, instance, "ToString()", format, reportAt: invocation);
            return;
        }

        // int.Parse(text), double.TryParse(text, out var d)
        if (method.Name is "Parse" or "TryParse" && method.IsStatic && types.IsCultureSensitive(method.ContainingType))
        {
            Report(context, invocation, $"{method.ContainingType.Name}.{method.Name}", method.ContainingType.ToDisplayString());
            return;
        }

        // string.Format("{0}", value), string.Concat(prefix, value), string.Join(", ", values)
        if (method.ContainingType.SpecialType == SpecialType.System_String && method.Name is "Format" or "Concat" or "Join")
        {
            // Join<T>(separator, IEnumerable<T>) formats every element with the current culture.
            if (method is { Name: "Join", IsGenericMethod: true } && types.IsCultureSensitive(method.TypeArguments[0]))
            {
                Report(context, invocation, "string.Join", method.TypeArguments[0].ToDisplayString());
                return;
            }

            // The format string / separator is text, never a formatted value.
            foreach (var argument in method.Name == "Concat" ? invocation.Arguments : invocation.Arguments.Skip(1))
            {
                ReportArgumentIfCultureSensitive(context, types, argument, $"string.{method.Name}");
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
                Report(context, invocation, $"Convert.{method.Name}", method.ReturnType.ToDisplayString());
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
        // string.Format("{0} {1}", a, b) passes a compiler-built params array: check its elements.
        if (argument.ArgumentKind == ArgumentKind.ParamArray
            && argument.Value is IArrayCreationOperation { Initializer: { } initializer })
        {
            foreach (var element in initializer.ElementValues)
            {
                ReportIfCultureSensitive(context, types, element, conversion);
            }

            return;
        }

        ReportIfCultureSensitive(context, types, argument.Value, conversion);
    }

    private static void ReportIfCultureSensitive(
        OperationAnalysisContext context,
        KnownTypes types,
        IOperation value,
        string conversion,
        IOperation? format = null,
        IOperation? reportAt = null)
    {
        var source = Unwrap(value);

        if (types.IsTypedConstantValue(source))
        {
            Report(context, reportAt ?? value, conversion, TypedConstantValueDisplay);
            return;
        }

        var type = UnderlyingType(source);

        // A non-negative integer constant ("_" + 1, $"{0}") formats the same in every culture: only the
        // negative sign varies for integers. That holds only without a culture-dependent format such as
        // "N2" (group and decimal separators). Negative and floating-point constants are still reported.
        if (IsNonNegativeIntegerConstant(source) && IsCultureNeutralIntegerFormat(format))
        {
            return;
        }

        // Hexadecimal formatting is culture-independent, but only integers support it:
        // "x.00" on a double is a custom format that still uses the culture's separator.
        if (types.IsCultureSensitive(type) && !(KnownTypes.IsIntegral(type!) && IsHexFormat(format)))
        {
            Report(context, reportAt ?? value, conversion, type!.ToDisplayString());
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

        return parent is IInterpolatedStringHandlerCreationOperation { HandlerCreation: IObjectCreationOperation creation }
            && creation.Arguments.Any(a => IsProvidedFormatProvider(a, types));
    }

    private static bool PassesFormatProvider(IInvocationOperation invocation, KnownTypes types)
        => invocation.Arguments.Any(a => IsProvidedFormatProvider(a, types));

    /// <summary>An <see cref="IFormatProvider"/> argument that is neither omitted nor <c>null</c>.</summary>
    private static bool IsProvidedFormatProvider(IArgumentOperation argument, KnownTypes types)
        => types.IsFormatProvider(argument.Parameter?.Type)
           && argument.ArgumentKind != ArgumentKind.DefaultValue
           && Unwrap(argument.Value).ConstantValue is not { HasValue: true, Value: null };

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

    private static bool IsNonNegativeIntegerConstant(IOperation operation)
        => operation.ConstantValue is { HasValue: true, Value: sbyte or short or int or long } constant
           && System.Convert.ToInt64(constant.Value, System.Globalization.CultureInfo.InvariantCulture) >= 0;

    /// <summary>No format, or a standard integer format whose output has no separators: D, G or X.</summary>
    private static bool IsCultureNeutralIntegerFormat(IOperation? format)
    {
        if (format is null)
        {
            return true;
        }

        if (format.ConstantValue is not { HasValue: true, Value: string text })
        {
            return false;
        }

        return text.Length == 0
            || "DdGgXx".IndexOf(text[0]) >= 0 && text.Skip(1).All(char.IsDigit);
    }

    private static bool IsHexFormat(IOperation? format)
        => format?.ConstantValue is { HasValue: true, Value: string { Length: > 0 } text } && (text[0] == 'x' || text[0] == 'X');

    /// <summary>
    /// Strips implicit conversions (boxing, widening) and maps the receiver inside a
    /// conditional access (<c>x.Value?.ToString()</c>) back to the expression it stands for.
    /// </summary>
    private static IOperation Unwrap(IOperation operation)
    {
        while (true)
        {
            switch (operation)
            {
                case IConversionOperation { IsImplicit: true } conversion:
                    operation = conversion.Operand;
                    continue;
                case IConditionalAccessInstanceOperation instance:
                    var access = instance.Parent;
                    while (access is not null and not IConditionalAccessOperation)
                    {
                        access = access.Parent;
                    }

                    if (access is IConditionalAccessOperation conditional)
                    {
                        operation = conditional.Operation;
                        continue;
                    }

                    return operation;
                default:
                    return operation;
            }
        }
    }

    /// <summary>The operand's type before any implicit boxing or widening, unwrapping Nullable&lt;T&gt;.</summary>
    private static ITypeSymbol? UnderlyingType(IOperation operation)
    {
        var type = Unwrap(operation).Type;

        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            type = nullable.TypeArguments[0];
        }

        return type;
    }

    private static void Report(OperationAnalysisContext context, IOperation operation, string conversion, string typeDisplay)
        => context.ReportDiagnostic(Diagnostic.Create(Rule, operation.Syntax.GetLocation(), conversion, typeDisplay));

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
            TypedConstantValue = compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.TypedConstant")?
                .GetMembers("Value").OfType<IPropertySymbol>().FirstOrDefault();

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
        private IPropertySymbol? TypedConstantValue { get; }

        public bool IsFormatProvider(ITypeSymbol? type)
            => type is not null && SymbolEqualityComparer.Default.Equals(type, FormatProvider);

        public bool IsTypedConstantValue(IOperation operation)
            => TypedConstantValue is not null
               && operation is IPropertyReferenceOperation reference
               && SymbolEqualityComparer.Default.Equals(reference.Property, TypedConstantValue);

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

        /// <summary>Signed integer types, the only culture-sensitive types that support hex formats.</summary>
        public static bool IsIntegral(ITypeSymbol type) => type.SpecialType is SpecialType.System_SByte
            or SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64 or SpecialType.System_IntPtr
            || type.Name == "BigInteger" && type.ContainingNamespace?.ToDisplayString() == "System.Numerics";
    }
}
