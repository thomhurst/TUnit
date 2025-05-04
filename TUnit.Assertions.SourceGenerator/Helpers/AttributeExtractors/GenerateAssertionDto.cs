// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.SourceGenerator.Helpers.AttributeExtractors;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class GenerateAssertionDto(
    Location? attributeLocation,
    ITypeSymbol typeArg,
    AssertionType assertionType,
    string methodName,
    string? messageFactoryMethodName,
    string? expectationExpression
) {
    public bool RequiresNullCheck => !typeArg.IsValueType;
    private readonly HashSet<char> _vowels = ['a', 'e', 'i', 'o', 'u'];
    private ISymbol? _memberSymbol;
    
    public string TypeName { get; } = GetTypeName(typeArg);

    private static string GetTypeName(ITypeSymbol typeArg) {
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (typeArg.SpecialType is
            SpecialType.System_Char
            or SpecialType.System_String
            or SpecialType.System_Boolean
            or SpecialType.System_Int32
            or SpecialType.System_Double
            or SpecialType.System_Single
            or SpecialType.System_Byte) {
            return typeArg.Name.ToLowerInvariant();
        }
        
        return typeArg.ToDisplayString();
    }

    public string GetMethodName() {
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return assertionType switch {
            AssertionType.Is => methodName,
            AssertionType.IsNot => methodName.Replace("Is", "IsNot"),
            _ => throw new ArgumentOutOfRangeException(nameof(assertionType), assertionType, null),
        };
    }

    public string GetMessageFactoryOrDefault() {
        if (!string.IsNullOrWhiteSpace(messageFactoryMethodName)) return messageFactoryMethodName!;

        string strippedMethodName = methodName.Replace("Is", "");
        string a = _vowels.Contains(strippedMethodName.ToLowerInvariant()[0]) ? "an" : "a";

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return assertionType switch {
            AssertionType.Is => $"(s, _, _) => $\"'{{s}}' was not {a} {strippedMethodName.ToSpaceSeperated()}\"",
            AssertionType.IsNot => $"(s, _, _) => $\"'{{s}}' was {a} {strippedMethodName.ToSpaceSeperated()}\"",
            _ => throw new ArgumentOutOfRangeException(nameof(assertionType), assertionType, null),
        };
    }

    public string GetExpectationExpressionOrDefault() {
        if (!string.IsNullOrWhiteSpace(expectationExpression)) return expectationExpression!;

        string strippedMethodName = methodName.Replace("Is", "");
        string a = _vowels.Contains(strippedMethodName.ToLowerInvariant()[0]) ? "an" : "a";

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return assertionType switch {
            AssertionType.Is => $"\"to be {a} {strippedMethodName.ToSpaceSeperated()}\"",
            AssertionType.IsNot => $"\"to not be {a} {strippedMethodName.ToSpaceSeperated()}\"",
            _ => throw new ArgumentOutOfRangeException(nameof(assertionType), assertionType, null),
        };
    }

    public string GetNullCheck() {
        if (!RequiresNullCheck) return "// No null check required";

        return $$"""
        if (value is null)
                    {
                        self.FailWithMessage("Actual {{TypeName}} is null");
                        return false;
                    }
        """;
    }

    public bool TryVerifyOrGetDiagnostics(out ImmutableArray<Diagnostic> diagnostics) {
        diagnostics = ImmutableArray<Diagnostic>.Empty;
        
        var members = typeArg.GetMembers(methodName);
        ISymbol? member = members.FirstOrDefault();
        
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder = ImmutableArray.CreateBuilder<Diagnostic>();
        switch (member) {
            case null: {
                diagnosticsBuilder.Add(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TESTING001",
                        "Member not found",
                        "Could not find member '{0}' in type '{1}'",
                        "TUnit",
                        DiagnosticSeverity.Error,
                        true),
                    attributeLocation ?? Location.None,
                    methodName, typeArg.Name)
                );
                break;
            }

            // Check if it's a property
            case IPropertySymbol { Type.SpecialType: not SpecialType.System_Boolean } propertySymbol: {
                diagnosticsBuilder.Add(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TESTING002",
                        "Invalid property return type",
                        "Property '{0}' must return bool, but returns {1}",
                        "TUnit",
                        DiagnosticSeverity.Error,
                        true),
                    attributeLocation ?? Location.None,
                    methodName, propertySymbol.Type));
                break;
            }
            
            // Check if it's a method
            case IMethodSymbol { ReturnType.SpecialType: not SpecialType.System_Boolean } methodSymbol: {
                diagnosticsBuilder.Add(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TESTING003",
                        "Invalid method return type",
                        "Method '{0}' must return bool, but returns {1}",
                        "TUnit",
                        DiagnosticSeverity.Error,
                        true),
                    attributeLocation ?? Location.None,
                    methodName, methodSymbol.ReturnType));
                break;
            }
            
            case IMethodSymbol methodSymbol when methodSymbol.Parameters.Length > (methodSymbol.IsExtensionMethod ? 1 : 0)
                && typeArg.SpecialType is not (
                    SpecialType.System_Char
                    or SpecialType.System_String
                    or SpecialType.System_Boolean
                    or SpecialType.System_Int32
                    or SpecialType.System_Double
                    or SpecialType.System_Single
                    or SpecialType.System_Byte
                ): {
                diagnosticsBuilder.Add(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TESTING004",
                        "Invalid method parameters",
                        "Method '{0}' must have no parameters",
                        "TUnit",
                        DiagnosticSeverity.Error,
                        true),
                    attributeLocation ?? Location.None,
                    methodName));
                break;
            }
            case not (IPropertySymbol or IMethodSymbol): {
                diagnosticsBuilder.Add(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TESTING005",
                        "Invalid member type",
                        "Member '{0}' must be either a property or a method",
                        "TUnit",
                        DiagnosticSeverity.Error,
                        true),
                    attributeLocation ?? Location.None,
                    methodName));
                break;
            }
        }

        if (!diagnosticsBuilder.Any()) {
            _memberSymbol = member;
            return true;
        }
        diagnostics = diagnosticsBuilder.ToImmutable();
        return false;

    }

    public string GetActualCheck() {
        if (_memberSymbol == null) throw new InvalidOperationException("TryVerifyOrGetDiagnostics must be called before GetActualCheck");
        
        // ReSharper disable once ConvertSwitchStatementToSwitchExpression
        switch (_memberSymbol, assertionType) {
            case (IMethodSymbol { IsStatic: true }, AssertionType.Is) : {
                return $"{TypeName}.{methodName}(value)"; 
            }

            case (IMethodSymbol, AssertionType.Is): {
                return $"value.{methodName}()";
            }
            
            case (IPropertySymbol, AssertionType.Is): {
                return $"value.{methodName}";
            }

            case (IMethodSymbol { IsStatic: true }, AssertionType.IsNot) : {
                return $"!{TypeName}.{methodName}(value)";
            }

            case (IMethodSymbol, AssertionType.IsNot): {
                return $"!value.{methodName}()";
            }
            
            case (IPropertySymbol, AssertionType.IsNot): {
                return $"!value.{methodName}";
            }

            default: {
                throw new ArgumentOutOfRangeException(nameof(assertionType), assertionType, null);
            }
        }
    }
}