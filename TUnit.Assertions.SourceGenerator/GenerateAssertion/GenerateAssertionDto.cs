using Microsoft.CodeAnalysis;
using TUnit.Assertions.SourceGenerator.Helpers;

namespace TUnit.Assertions.SourceGenerator.GenerateAssertion;

public record GenerateAssertionDto(
    Location? AttributeLocation,
    ITypeSymbol TypeArg,
    AssertionType AssertionType,
    string MethodName,
    string? MessageFactoryMethodName,
    string? ExpectationExpression
) {
    private bool RequiresNullCheck { get; } = !TypeArg.IsValueType;
    public string TypeName { get; } = GetTypeName(TypeArg);
    
    private readonly HashSet<char> _vowels = ['a', 'e', 'i', 'o', 'u'];
    private readonly ISymbol? _memberSymbol = TypeArg.GetMembers(MethodName).FirstOrDefault();

    private static string GetTypeName(ITypeSymbol typeArg) =>
        typeArg.SpecialType switch {
            SpecialType.System_Char 
            or SpecialType.System_String 
            or SpecialType.System_Boolean 
            or SpecialType.System_Int32 
            or SpecialType.System_Double 
            or SpecialType.System_Single 
            or SpecialType.System_Byte => typeArg.Name.ToLowerInvariant(),
            _ => typeArg.ToDisplayString()
        };

    public string GetMethodName() =>
        AssertionType switch {
            AssertionType.Is => MethodName,
            AssertionType.IsNot => MethodName.Replace("Is", "IsNot"),
            _ => throw new ArgumentOutOfRangeException(nameof(AssertionType), AssertionType, null),
        };

    public string GetMessageFactoryOrDefault() {
        if (!string.IsNullOrWhiteSpace(MessageFactoryMethodName)) return MessageFactoryMethodName!;

        string strippedMethodName = MethodName.Replace("Is", "");
        string a = _vowels.Contains(strippedMethodName.ToLowerInvariant()[0]) ? "an" : "a";

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return AssertionType switch {
            AssertionType.Is => $"(s, _, _) => $\"'{{s}}' was not {a} {strippedMethodName.ToSpaceSeperated()}\"",
            AssertionType.IsNot => $"(s, _, _) => $\"'{{s}}' was {a} {strippedMethodName.ToSpaceSeperated()}\"",
            _ => throw new ArgumentOutOfRangeException(nameof(AssertionType), AssertionType, null),
        };
    }

    public string GetExpectationExpressionOrDefault() {
        if (!string.IsNullOrWhiteSpace(ExpectationExpression)) return ExpectationExpression!;

        string strippedMethodName = MethodName.Replace("Is", "");
        string a = _vowels.Contains(strippedMethodName.ToLowerInvariant()[0]) ? "an" : "a";

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return AssertionType switch {
            AssertionType.Is => $"\"to be {a} {strippedMethodName.ToSpaceSeperated()}\"",
            AssertionType.IsNot => $"\"to not be {a} {strippedMethodName.ToSpaceSeperated()}\"",
            _ => throw new ArgumentOutOfRangeException(nameof(AssertionType), AssertionType, null),
        };
    }

    public string GetNullCheck() {
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (!RequiresNullCheck) return "// No null check required";

        return $$"""
        if (value is null)
                    {
                        self.FailWithMessage("Actual {{TypeName}} is null");
                        return false;
                    }
        """;
    }
    
    public string GetActualCheck() {
        if (_memberSymbol == null) throw new InvalidOperationException("TryVerifyOrGetDiagnostics must be called before GetActualCheck");
        
        // ReSharper disable once ConvertSwitchStatementToSwitchExpression
        switch (_memberSymbol, assertionType: AssertionType) {
            case (IMethodSymbol { IsStatic: true }, AssertionType.Is) : {
                return $"{TypeName}.{MethodName}(value)"; 
            }

            case (IMethodSymbol, AssertionType.Is): {
                return $"value.{MethodName}()";
            }
            
            case (IPropertySymbol, AssertionType.Is): {
                return $"value.{MethodName}";
            }

            case (IMethodSymbol { IsStatic: true }, AssertionType.IsNot) : {
                return $"!{TypeName}.{MethodName}(value)";
            }

            case (IMethodSymbol, AssertionType.IsNot): {
                return $"!value.{MethodName}()";
            }
            
            case (IPropertySymbol, AssertionType.IsNot): {
                return $"!value.{MethodName}";
            }

            default: {
                throw new ArgumentOutOfRangeException(nameof(AssertionType), AssertionType, null);
            }
        }
    }
}