using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

/// <summary>
/// An argument of a generated method call. <see cref="Name"/> is set when the argument must be
/// passed by name (e.g. a trailing array after omitted optional parameters).
/// </summary>
public readonly record struct InvocationArgument(string Expression, string? Name = null)
{
    public bool IsNamed => Name is not null;

    public override string ToString() => Name is null ? Expression : $"@{Name}: {Expression}";
}

public static class TupleArgumentHelper
{
    /// <summary>
    /// Generates method invocation arguments.
    /// </summary>
    /// <param name="parameters">The method parameters</param>
    /// <param name="argumentsArrayName">The name of the arguments array</param>
    /// <returns>Comma-separated argument expressions for method invocation</returns>
    public static string GenerateMethodInvocationArguments(IList<IParameterSymbol> parameters, string argumentsArrayName)
    {
        var allArguments = new List<string>();

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var castExpression = $"global::TUnit.Core.Helpers.CastHelper.Cast<{parameter.Type.GloballyQualified()}>({argumentsArrayName}[{i.ToString(CultureInfo.InvariantCulture)}])";
            allArguments.Add(castExpression);
        }

        return string.Join(", ", allArguments);
    }

    /// <summary>
    /// Generates argument access for a method with possible params array, given a specific argument count.
    /// </summary>
    /// <param name="parameters">The method parameters</param>
    /// <param name="argumentsArrayName">The name of the arguments array</param>
    /// <param name="argumentCount">The actual number of arguments provided</param>
    /// <returns>The arguments for the method invocation, in declaration order</returns>
    public static List<InvocationArgument> GenerateArgumentAccessWithParams(IList<IParameterSymbol> parameters, string argumentsArrayName, object argumentCount)
    {
        var argumentExpressions = new List<InvocationArgument>();

        // Parse argument count - can be an int or a string expression
        int argCount;
        string? argCountExpression = null;
        if (argumentCount is int count)
        {
            argCount = count;
        }
        else if (argumentCount is string expr)
        {
            argCountExpression = expr;
            argCount = int.MaxValue; // Flag for dynamic count
        }
        else
        {
            throw new ArgumentException("argumentCount must be int or string");
        }

        // A trailing array parameter (params OR a plain `T[]`) collects remaining loose values
        // into the array, letting `[Arguments(["a", "b"])]` map onto a single `string[]`
        // parameter (issue #6120).
        var hasParams = parameters.Count > 0 && parameters[parameters.Count - 1].CollectsTrailingArguments();

        if (!hasParams)
        {
            // No params array - just cast each argument
            if (argCountExpression != null)
            {
                // Dynamic count - use Math.Min
                for (var i = 0; i < parameters.Count; i++)
                {
                    var param = parameters[i];
                    var castExpression = $"global::TUnit.Core.Helpers.CastHelper.Cast<{param.Type.GloballyQualified()}>({argumentsArrayName}[{i.ToString(CultureInfo.InvariantCulture)}])";
                    argumentExpressions.Add(new InvocationArgument(castExpression));
                }
            }
            else
            {
                for (var i = 0; i < parameters.Count && i < argCount; i++)
                {
                    var param = parameters[i];
                    var castExpression = $"global::TUnit.Core.Helpers.CastHelper.Cast<{param.Type.GloballyQualified()}>({argumentsArrayName}[{i.ToString(CultureInfo.InvariantCulture)}])";
                    argumentExpressions.Add(new InvocationArgument(castExpression));
                }

                TryAppendOmittedOptionalDefaults(parameters, argCount, parameters.Count, argumentExpressions);
            }
        }
        else
        {
            // Has params array
            var regularParamCount = parameters.Count - 1;

            // Handle regular parameters
            if (argCountExpression != null)
            {
                // Dynamic count. Every regular parameter gets an argument, so no optional is left out and
                // the call cannot bind to a sibling overload (#7122). This arm only runs for counts past
                // the statically generated cases, which already supply every regular parameter.
                for (var i = 0; i < regularParamCount; i++)
                {
                    var param = parameters[i];
                    var castExpression = $"global::TUnit.Core.Helpers.CastHelper.Cast<{param.Type.GloballyQualified()}>({argumentsArrayName}[{i.ToString(CultureInfo.InvariantCulture)}])";
                    argumentExpressions.Add(new InvocationArgument(castExpression));
                }
            }
            else
            {
                for (var i = 0; i < regularParamCount && i < argCount; i++)
                {
                    var param = parameters[i];
                    var castExpression = $"global::TUnit.Core.Helpers.CastHelper.Cast<{param.Type.GloballyQualified()}>({argumentsArrayName}[{i.ToString(CultureInfo.InvariantCulture)}])";
                    argumentExpressions.Add(new InvocationArgument(castExpression));
                }

                TryAppendOmittedOptionalDefaults(parameters, argCount, regularParamCount, argumentExpressions);
            }

            // Handle params array parameter
            var paramsParam = parameters[parameters.Count - 1];
            var elementType = GetTrailingElementType(paramsParam.Type);

            if (elementType != null)
            {
                if (argCountExpression != null)
                {
                    // Dynamic count - create array from remaining arguments. With no leading
                    // regular parameters the range spans the whole args array, so drop the
                    // redundant "- 0" offset for cleaner generated code.
                    var rangeCount = regularParamCount == 0
                        ? argCountExpression
                        : $"{argCountExpression} - {regularParamCount.ToString(CultureInfo.InvariantCulture)}";
                    // Use fully-qualified static Enumerable.Select/ToArray calls rather than the
                    // extension-method chain — generated files don't import System.Linq unless the
                    // project has ImplicitUsings enabled, so the extension form fails to compile (CS1061).
                    var arrayInit = $"({argumentsArrayName}.Length > {regularParamCount.ToString(CultureInfo.InvariantCulture)} ? global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Range({regularParamCount.ToString(CultureInfo.InvariantCulture)}, {rangeCount}), i => global::TUnit.Core.Helpers.CastHelper.Cast<{elementType.GloballyQualified()}>({argumentsArrayName}[i]))) : global::System.Array.Empty<{elementType.GloballyQualified()}>())";
                    argumentExpressions.Add(new InvocationArgument(arrayInit));
                }
                else
                {
                    var remainingArgCount = Math.Max(0, argCount - regularParamCount);

                    // When optional regular parameters were omitted (argCount < regularParamCount) and
                    // their defaults could not be written out, a positional array would bind to the
                    // first omitted parameter, so name it.
                    var arrayName = argumentExpressions.Count < regularParamCount
                        ? paramsParam.Name
                        : null;

                    if (remainingArgCount == 0 && !paramsParam.IsParams && paramsParam.HasExplicitDefaultValue)
                    {
                        // An omitted optional plain array keeps its declared default (as C# and
                        // reflection mode do). The only default an array parameter can declare is
                        // null, so pass a typed null rather than leaving the argument out: omitting
                        // it could bind the call to another overload (e.g. a parameterless M()).
                        argumentExpressions.Add(new InvocationArgument($"default({paramsParam.Type.GloballyQualified()})", arrayName));
                    }
                    else if (remainingArgCount == 0)
                    {
                        // No arguments for params array - pass empty array. Use Array.Empty<T>()
                        // rather than `new T[0]`: when T is itself an array (jagged param like
                        // byte[][], element type byte[]) the rank-specifier form `new byte[][0]`
                        // is invalid C# (CS1586/CS0178). See issue #6150.
                        argumentExpressions.Add(new InvocationArgument($"global::System.Array.Empty<{elementType.GloballyQualified()}>()", arrayName));
                    }
                    else if (remainingArgCount == 1)
                    {
                        // Single argument for params - check if it's null or already the correct array type
                        // In C#, params T[] can receive:
                        // - null (passed as null, not as array with null element)
                        // - T[] (passed directly, not wrapped in another array)
                        // - T (wrapped in array with single element)
                        // Anything else goes through CastHelper.ToTrailingArray, which also converts an
                        // array of a different runtime type element-wise (e.g. an object[] supplied by a
                        // MatrixAttribute subclass for a MyEnum[] parameter — issue #6678).
                        var singleArg = $"{argumentsArrayName}[{regularParamCount.ToString(CultureInfo.InvariantCulture)}]";
                        var checkAndCast = $"({singleArg} is null ? null : {singleArg} is {paramsParam.Type.GloballyQualified()} arr ? arr : global::TUnit.Core.Helpers.CastHelper.ToTrailingArray<{elementType.GloballyQualified()}>({singleArg}))";
                        argumentExpressions.Add(new InvocationArgument(checkAndCast));
                    }
                    else
                    {
                        // Multiple arguments for params - create array
                        var arrayElements = new List<string>();
                        for (var i = regularParamCount; i < argCount; i++)
                        {
                            arrayElements.Add($"global::TUnit.Core.Helpers.CastHelper.Cast<{elementType.GloballyQualified()}>({argumentsArrayName}[{i.ToString(CultureInfo.InvariantCulture)}])");
                        }
                        // Name the full array type rather than appending [] to the element type:
                        // for a multidimensional element (params int[][,]) `new int[,][]` would
                        // reverse the ranks and not compile. Collection interfaces still need T[].
                        var arrayType = paramsParam.Type is IArrayTypeSymbol
                            ? paramsParam.Type.GloballyQualified()
                            : $"{elementType.GloballyQualified()}[]";
                        argumentExpressions.Add(new InvocationArgument($"new {arrayType} {{ {string.Join(", ", arrayElements)} }}"));
                    }
                }
            }
            else
            {
                // Fallback if we can't determine element type
                var castExpression = $"global::TUnit.Core.Helpers.CastHelper.Cast<{paramsParam.Type.GloballyQualified()}>({argumentsArrayName}[{regularParamCount.ToString(CultureInfo.InvariantCulture)}])";
                argumentExpressions.Add(new InvocationArgument(castExpression));
            }
        }

        return argumentExpressions;
    }

    /// <summary>
    /// Passes the declared default of each omitted optional parameter in
    /// [<paramref name="start"/>, <paramref name="end"/>) explicitly. Leaving them out could bind the
    /// generated call to another overload that lacks those parameters (issue #7122), whereas an
    /// explicit argument for every parameter can only bind to the test method. This also matches
    /// reflection mode, which invokes with <see cref="System.Type.Missing"/> and so receives the
    /// declared defaults (including for caller-info parameters). All-or-nothing: if any default
    /// cannot be written as code, nothing is appended and the caller falls back to omission.
    /// </summary>
    private static void TryAppendOmittedOptionalDefaults(IList<IParameterSymbol> parameters, int start, int end, List<InvocationArgument> argumentExpressions)
    {
        if (start >= end)
        {
            return;
        }

        var defaults = new string[end - start];
        for (var i = start; i < end; i++)
        {
            if (!TryFormatDefaultValue(parameters[i], out var expression))
            {
                return;
            }

            defaults[i - start] = expression;
        }

        foreach (var expression in defaults)
        {
            argumentExpressions.Add(new InvocationArgument(expression));
        }
    }

    private static bool TryFormatDefaultValue(IParameterSymbol parameter, out string expression)
    {
        expression = string.Empty;

        if (!parameter.HasExplicitDefaultValue || parameter.RefKind != RefKind.None)
        {
            return false;
        }

        var type = parameter.Type;
        // A `dynamic` argument would make the whole call late-bound, so the runtime binder could pick a
        // sibling overload (and the call would need Microsoft.CSharp, which is not AOT-safe). `object` is
        // the same runtime type and keeps the call bound to the test method at compile time.
        var typeName = type.TypeKind == TypeKind.Dynamic ? "object" : type.GloballyQualified();
        var value = parameter.ExplicitDefaultValue;

        if (value is null)
        {
            // `= null` / `= default` / `= new S()`. The null-forgiving operator keeps a `string s = null`
            // style declaration from raising a nullable warning in the generated code.
            expression = $"default({typeName})!";
            return true;
        }

        // Non-finite values have no literal form. Name the constant on the value's own type, so an
        // `[DefaultParameterValue(float.NaN)] object x` default still boxes a float, as in reflection mode.
        var nonFinite = value switch
        {
            double d when double.IsNaN(d) => "double.NaN",
            double d when double.IsPositiveInfinity(d) => "double.PositiveInfinity",
            double d when double.IsNegativeInfinity(d) => "double.NegativeInfinity",
            float f when float.IsNaN(f) => "float.NaN",
            float f when float.IsPositiveInfinity(f) => "float.PositiveInfinity",
            float f when float.IsNegativeInfinity(f) => "float.NegativeInfinity",
            _ => null
        };

        if (nonFinite is not null)
        {
            expression = $"({typeName})({nonFinite})";
            return true;
        }

        var literal = SymbolDisplay.FormatPrimitive(value, quoteStrings: true, useHexadecimalNumbers: false);
        if (literal is null)
        {
            return false;
        }

        literal = value switch
        {
            float => literal + "F",
            double => literal + "D",
            decimal => literal + "M",
            _ => literal
        };

        // The cast restores enum and narrower numeric types (the stored value is the underlying constant),
        // and keeps a broader declared type: `[DefaultParameterValue("s")] object x` must not pass a bare literal,
        // which would let a sibling `M(string)` overload win resolution.
        expression = $"({typeName})({literal})";
        return true;
    }

    /// <summary>
    /// Gets the element type for a trailing collecting parameter. Handles plain arrays as well as
    /// C# 13 <c>params</c> collections whose target is an array-assignable generic interface
    /// (<c>IEnumerable&lt;T&gt;</c>, <c>IReadOnlyList&lt;T&gt;</c>, etc.) — for those a generated
    /// <c>T[]</c> satisfies the parameter. Returns null when no safe array construction applies.
    /// </summary>
    private static ITypeSymbol? GetTrailingElementType(ITypeSymbol parameterType)
    {
        if (parameterType is IArrayTypeSymbol { IsSZArray: true } array)
        {
            return array.ElementType;
        }

        if (parameterType is INamedTypeSymbol { IsGenericType: true } named &&
            IsArrayAssignableCollectionInterface(named.OriginalDefinition.SpecialType))
        {
            return named.TypeArguments[0];
        }

        return null;
    }

    private static bool IsArrayAssignableCollectionInterface(SpecialType specialType)
        => specialType is SpecialType.System_Collections_Generic_IEnumerable_T
            or SpecialType.System_Collections_Generic_ICollection_T
            or SpecialType.System_Collections_Generic_IList_T
            or SpecialType.System_Collections_Generic_IReadOnlyCollection_T
            or SpecialType.System_Collections_Generic_IReadOnlyList_T;
}
