// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.SourceGenerator;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class AssertionMethodDto {
    public required string TypeName { get; init; }
    public required string MethodName { get; init; }
    public required bool RequiresNullCheck { get; init; }
    
    public string ExpectedMessage => $"to be {MethodName.Replace("Is", "")} ";
    public string ActualMessage => $"'{{s}}' was not a {MethodName.Replace("Is", "")} ";
    public string NullCheck => RequiresNullCheck
        ? $$"""
                        if (value is null)
                        {
                            self.FailWithMessage("Actual {{TypeName}} is null");
                            return false;
                        }
                        
        """ 
        : "";

    public static AssertionMethodDto? FromIsAssertion(GenerateAssertionDto origin, AttributeData attribute) {
        ITypeSymbol? typeArgument = attribute.AttributeClass?.TypeArguments.FirstOrDefault();
        if (typeArgument is null) return null;
        string typeName = typeArgument.Name;
        if (typeArgument.SpecialType is 
            SpecialType.System_Char or 
            SpecialType.System_String or 
            SpecialType.System_Boolean or 
            SpecialType.System_Int32 or 
            SpecialType.System_Double or
            SpecialType.System_Single or
            SpecialType.System_Byte)
        {
            typeName = typeName.ToLowerInvariant();
        }
        
        bool canBeNull = !typeArgument.IsValueType || typeArgument.NullableAnnotation == NullableAnnotation.Annotated;

        string? methodName = attribute.ConstructorArguments[0].Value as string;
        if (string.IsNullOrEmpty(methodName)) return null;
        
        return new AssertionMethodDto { TypeName = typeName, MethodName = methodName!, RequiresNullCheck = canBeNull };
    }
}
