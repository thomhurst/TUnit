// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
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
    public bool RequiresNullCheck =>  !typeArg.IsValueType;
    private readonly HashSet<char> _vowels = ['a', 'e', 'i', 'o', 'u'];
    
    public string GetTypeName() {
        string typeName = typeArg.Name;
        if (typeArg.SpecialType is 
            SpecialType.System_Char 
            or SpecialType.System_String 
            or SpecialType.System_Boolean 
            or SpecialType.System_Int32 
            or SpecialType.System_Double 
            or SpecialType.System_Single 
            or SpecialType.System_Byte)
        {
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

    public string GetActualCheck() {
        // ReSharper disable once ConvertSwitchStatementToSwitchExpression
        switch (assertionType, typeArg.SpecialType) {

            case (AssertionType.Is, SpecialType.System_Char):
            case (AssertionType.Is, SpecialType.System_String):
            case (AssertionType.Is, SpecialType.System_Boolean):
            case (AssertionType.Is, SpecialType.System_Int32):
            case (AssertionType.Is, SpecialType.System_Double):
            case (AssertionType.Is, SpecialType.System_Single):
            case (AssertionType.Is, SpecialType.System_Byte): {
                return $"{GetTypeName()}.{methodName}(value)";
            }

            case (AssertionType.Is, _): {
                return $"value.{methodName}()";
            }
            
            case (AssertionType.IsNot, SpecialType.System_Char):
            case (AssertionType.IsNot, SpecialType.System_String):
            case (AssertionType.IsNot, SpecialType.System_Boolean):
            case (AssertionType.IsNot, SpecialType.System_Int32):
            case (AssertionType.IsNot, SpecialType.System_Double):
            case (AssertionType.IsNot, SpecialType.System_Single):
            case (AssertionType.IsNot, SpecialType.System_Byte): {
                return $"!{GetTypeName()}.{methodName}(value)";
            }

            case (AssertionType.IsNot, _): {
                return $"!value.{methodName}()";
            }

            default: {
                throw new ArgumentOutOfRangeException(nameof(assertionType), assertionType, null);
            }
        }
        
    }
}
