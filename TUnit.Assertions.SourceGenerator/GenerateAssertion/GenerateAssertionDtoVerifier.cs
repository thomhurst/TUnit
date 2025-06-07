// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace TUnit.Assertions.SourceGenerator.GenerateAssertion;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class GenerateAssertionDtoVerifier {
    public static bool TryVerifyOrGetDiagnostics(GenerateAssertionDto dto, out ImmutableArray<Diagnostic> diagnostics) {
        diagnostics = ImmutableArray<Diagnostic>.Empty;
        
        ImmutableArray<ISymbol> members = dto.TypeArg.GetMembers(dto.MethodName);
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
                    dto.AttributeLocation ?? Location.None,
                    dto.MethodName, dto.TypeArg.Name)
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
                    dto.AttributeLocation ?? Location.None,
                    dto.MethodName, propertySymbol.Type));
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
                    dto.AttributeLocation ?? Location.None,
                    dto.MethodName, methodSymbol.ReturnType));
                break;
            }
            
            case IMethodSymbol methodSymbol when methodSymbol.Parameters.Length > (methodSymbol.IsExtensionMethod ? 1 : 0)
                && dto.TypeArg.SpecialType is not (
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
                    dto.AttributeLocation ?? Location.None,
                    dto.MethodName));
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
                    dto.AttributeLocation ?? Location.None,
                    dto.MethodName));
                break;
            }
        }

        if (!diagnosticsBuilder.Any()) return true;

        diagnostics = diagnosticsBuilder.ToImmutable();
        return false;
    }
}
