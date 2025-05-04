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
    ITypeSymbol typeArg,
    AssertionType assertionType,
    string methodName,
    string? messageFactoryMethodName,
    string? expectationExpression
) {
    public bool RequiresNullCheck => !typeArg.IsValueType;
    private readonly HashSet<char> _vowels = ['a', 'e', 'i', 'o', 'u'];
    private ISymbol? _memberSymbol;

    public string GetTypeName() {
        string typeName = typeArg.Name;
        if (typeArg.SpecialType is
            SpecialType.System_Char
            or SpecialType.System_String
            or SpecialType.System_Boolean
            or SpecialType.System_Int32
            or SpecialType.System_Double
            or SpecialType.System_Single
            or SpecialType.System_Byte) {
            typeName = typeName.ToLowerInvariant();
        }

        return typeName;
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
            AssertionType.Is => $"(s, _, _) => \"'{{s}}' was not {a} {strippedMethodName}\"",
            AssertionType.IsNot => $"(s, _, _) => \"'{{s}}' was {a} {strippedMethodName}\"",
            _ => throw new ArgumentOutOfRangeException(nameof(assertionType), assertionType, null),
        };
    }

    public string GetExpectationExpressionOrDefault() {
        if (!string.IsNullOrWhiteSpace(expectationExpression)) return expectationExpression!;

        string strippedMethodName = methodName.Replace("Is", "");
        string a = _vowels.Contains(strippedMethodName[0]) ? "an" : "a";

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return assertionType switch {
            AssertionType.Is => $"\"to be {a} {strippedMethodName}\"",
            AssertionType.IsNot => $"\"to not be {a} {strippedMethodName}\"",
            _ => throw new ArgumentOutOfRangeException(nameof(assertionType), assertionType, null),
        };
    }

    public string GetNullCheck() {
        if (!RequiresNullCheck) return "";
        string typeName = GetTypeName();

        return $$"""
                 if (value is null)
                 {
                     self.FailWithMessage("Actual {{typeName}} is null");
                     return false;
                 }
                 """;
    }

    public bool TryVerify(out ImmutableArray<Diagnostic> diagnostics) {
        diagnostics = ImmutableArray<Diagnostic>.Empty;
        
        var members = typeArg.GetMembers(methodName);
        ISymbol? member = members.FirstOrDefault();
        
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder = ImmutableArray.CreateBuilder<Diagnostic>();
        switch (member) {
            case null: {
                diagnosticsBuilder.Add(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TESTING-001",
                        "Member not found",
                        "Could not find member '{0}' in type '{1}'",
                        "TUnit",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None,
                    methodName, typeArg.Name)
                );
                break;
            }
                

            // Check if it's a property
            case IPropertySymbol propertySymbol when propertySymbol.Type.SpecialType != SpecialType.System_Boolean: {
                diagnosticsBuilder.Add(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TUNIT002",
                        "Invalid property return type",
                        "Property '{0}' must return bool, but returns {1}",
                        "TUnit",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None,
                    methodName, propertySymbol.Type));
                break;
            }
            // Check if it's a method
            case IMethodSymbol methodSymbol when methodSymbol.ReturnType.SpecialType != SpecialType.System_Boolean: {
                diagnosticsBuilder.Add(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TUNIT003",
                        "Invalid method return type",
                        "Method '{0}' must return bool, but returns {1}",
                        "TUnit",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None,
                    methodName, methodSymbol.ReturnType));
                break;
            }
            case IMethodSymbol methodSymbol when methodSymbol.Parameters.Length > 0: {
                diagnosticsBuilder.Add(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TUNIT004",
                        "Invalid method parameters",
                        "Method '{0}' must have no parameters",
                        "TUnit",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None,
                    methodName));
                break;
            }
            case not (IPropertySymbol or IMethodSymbol): {
                diagnosticsBuilder.Add(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TUNIT005",
                        "Invalid member type",
                        "Member '{0}' must be either a property or a method",
                        "TUnit",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None,
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
        if (_memberSymbol == null) throw new InvalidOperationException("TryVerify must be called before GetActualCheck");
        
        // ReSharper disable once ConvertSwitchStatementToSwitchExpression
        switch (_memberSymbol, assertionType, typeArg.SpecialType) {
            case (IMethodSymbol, AssertionType.Is, SpecialType.System_Char):
            case (IMethodSymbol, AssertionType.Is, SpecialType.System_String):
            case (IMethodSymbol, AssertionType.Is, SpecialType.System_Boolean):
            case (IMethodSymbol, AssertionType.Is, SpecialType.System_Int32):
            case (IMethodSymbol, AssertionType.Is, SpecialType.System_Double):
            case (IMethodSymbol, AssertionType.Is, SpecialType.System_Single):
            case (IMethodSymbol, AssertionType.Is, SpecialType.System_Byte): {
                return $"{GetTypeName()}.{methodName}(value)";
            }

            case (IMethodSymbol, AssertionType.Is, _): {
                return $"value.{methodName}()";
            }
            
            case (IPropertySymbol, AssertionType.Is, _): {
                return $"value.{methodName}";
            }

            case (IMethodSymbol, AssertionType.IsNot, SpecialType.System_Char):
            case (IMethodSymbol, AssertionType.IsNot, SpecialType.System_String):
            case (IMethodSymbol, AssertionType.IsNot, SpecialType.System_Boolean):
            case (IMethodSymbol, AssertionType.IsNot, SpecialType.System_Int32):
            case (IMethodSymbol, AssertionType.IsNot, SpecialType.System_Double):
            case (IMethodSymbol, AssertionType.IsNot, SpecialType.System_Single):
            case (IMethodSymbol, AssertionType.IsNot, SpecialType.System_Byte): {
                return $"!{GetTypeName()}.{methodName}(value)";
            }

            case (IMethodSymbol, AssertionType.IsNot, _): {
                return $"!value.{methodName}()";
            }
            
            case (IPropertySymbol, AssertionType.IsNot, _): {
                return $"!value.{methodName}";
            }

            default: {
                throw new ArgumentOutOfRangeException(nameof(assertionType), assertionType, null);
            }
        }
    }
}