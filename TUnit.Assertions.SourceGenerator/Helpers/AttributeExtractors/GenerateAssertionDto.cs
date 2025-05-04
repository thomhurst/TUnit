// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.SourceGenerator.Helpers.AttributeExtractors;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record GenerateAssertionDto(
    ITypeSymbol TypeArg,
    AssertionType Type,
    string MethodName,
    string? MessageFactoryMethodName,
    string? ExpectationExpression
) {
    public bool RequiresNullCheck =>  !TypeArg.IsValueType;
    private readonly HashSet<char> _vowels = ['a', 'e', 'i', 'o', 'u'];
    
    public string GetTypeName() {
        string typeName = TypeArg.Name;
        if (TypeArg.SpecialType is 
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

    public string GetMessageFactoryOrDefault() {
        if (!string.IsNullOrWhiteSpace(MessageFactoryMethodName)) return MessageFactoryMethodName!;
        
        string strippedMethodName = MethodName.Replace("Is", "");
        string a = _vowels.Contains(strippedMethodName[0]) ? "an" : "a";
        return $"(s, _, _) => \"'{{s}}' was not {a} {strippedMethodName}\"";
    }
    
    public string GetExpectationExpressionOrDefault() {
        if (!string.IsNullOrWhiteSpace(ExpectationExpression)) return ExpectationExpression!;
        
        string strippedMethodName = MethodName.Replace("Is", "");
        string a = _vowels.Contains(strippedMethodName[0]) ? "an" : "a";
        return $"\"to be {a} {strippedMethodName}\"";
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
        if (TypeArg.SpecialType is
            SpecialType.System_Char
            or SpecialType.System_String
            or SpecialType.System_Boolean
            or SpecialType.System_Int32
            or SpecialType.System_Double
            or SpecialType.System_Single
            or SpecialType.System_Byte) {
            
            return $"{GetTypeName()}.{MethodName}(value)";
        }
        
        return $"value.{MethodName}()";
    }
}
