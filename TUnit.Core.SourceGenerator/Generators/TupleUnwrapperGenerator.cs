using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Generates compile-time tuple unwrapping code to eliminate reflection
/// </summary>
public sealed class TupleUnwrapperGenerator
{
    /// <summary>
    /// Generates tuple unwrapping methods for all tuple types used in test data
    /// </summary>
    public void GenerateTupleUnwrappers(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        var tupleTypes = CollectTupleTypes(testMethods);
        
        if (!tupleTypes.Any())
        {
            return;
        }

        writer.AppendLine("// Generated tuple unwrapping methods");
        writer.AppendLine("internal static class TupleUnwrappers");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var tupleType in tupleTypes)
        {
            GenerateTupleUnwrapper(writer, tupleType);
        }

        writer.Unindent();
        writer.AppendLine("}");
    }

    private HashSet<INamedTypeSymbol> CollectTupleTypes(IEnumerable<TestMethodMetadata> testMethods)
    {
        var tupleTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var testMethod in testMethods)
        {
            // Check method parameters for tuple types
            foreach (var param in testMethod.MethodSymbol.Parameters)
            {
                CollectTupleTypesFromType(param.Type, tupleTypes);
            }

            // Check Arguments attributes for tuple values
            var argumentsAttrs = testMethod.MethodSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute");

            foreach (var attr in argumentsAttrs)
            {
                foreach (var arg in attr.ConstructorArguments)
                {
                    if (arg.Kind == TypedConstantKind.Array)
                    {
                        foreach (var value in arg.Values)
                        {
                            if (value.Value != null && value.Type is INamedTypeSymbol { IsTupleType: true } tupleType)
                            {
                                tupleTypes.Add(tupleType);
                            }
                        }
                    }
                    else if (arg.Value != null && arg.Type is INamedTypeSymbol { IsTupleType: true } tupleType)
                    {
                        tupleTypes.Add(tupleType);
                    }
                }
            }
        }

        return tupleTypes;
    }

    private void CollectTupleTypesFromType(ITypeSymbol type, HashSet<INamedTypeSymbol> tupleTypes)
    {
        if (type is INamedTypeSymbol { IsTupleType: true } tupleType)
        {
            tupleTypes.Add(tupleType);
            
            // Recursively check tuple elements for nested tuples
            foreach (var element in tupleType.TupleElements)
            {
                CollectTupleTypesFromType(element.Type, tupleTypes);
            }
        }
        else if (type is IArrayTypeSymbol arrayType)
        {
            CollectTupleTypesFromType(arrayType.ElementType, tupleTypes);
        }
        else if (type is INamedTypeSymbol { IsGenericType: true } genericType)
        {
            foreach (var typeArg in genericType.TypeArguments)
            {
                CollectTupleTypesFromType(typeArg, tupleTypes);
            }
        }
    }

    private void GenerateTupleUnwrapper(CodeWriter writer, INamedTypeSymbol tupleType)
    {
        var elements = tupleType.TupleElements;
        var methodName = GetUnwrapperMethodName(tupleType);
        var tupleTypeName = tupleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        writer.AppendLine($"public static void {methodName}(object value, object?[] args)");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"var tuple = ({tupleTypeName})value;");
        
        for (int i = 0; i < elements.Length; i++)
        {
            var element = elements[i];
            var fieldName = element.IsExplicitlyNamedTupleElement ? element.Name : $"Item{i + 1}";
            
            if (element.Type is INamedTypeSymbol { IsTupleType: true } nestedTuple)
            {
                // For nested tuples, recursively unwrap
                var nestedMethodName = GetUnwrapperMethodName(nestedTuple);
                writer.AppendLine($"var tempArgs = new object?[{nestedTuple.TupleElements.Length}];");
                writer.AppendLine($"{nestedMethodName}(tuple.{fieldName}, tempArgs);");
                
                // Copy unwrapped values to the main args array
                for (int j = 0; j < nestedTuple.TupleElements.Length; j++)
                {
                    writer.AppendLine($"args[{i + j}] = tempArgs[{j}];");
                }
            }
            else
            {
                writer.AppendLine($"args[{i}] = tuple.{fieldName};");
            }
        }

        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }

    private string GetUnwrapperMethodName(INamedTypeSymbol tupleType)
    {
        var elements = tupleType.TupleElements;
        var typeNames = elements.Select(e => GetSimpleTypeName(e.Type)).ToArray();
        return $"UnwrapTuple_{string.Join("_", typeNames)}";
    }

    private string GetSimpleTypeName(ITypeSymbol type)
    {
        return type switch
        {
            INamedTypeSymbol { IsTupleType: true } tuple => $"Tuple{tuple.TupleElements.Length}",
            IArrayTypeSymbol array => $"{GetSimpleTypeName(array.ElementType)}Array",
            INamedTypeSymbol named when named.IsGenericType => $"{named.Name}{named.TypeArguments.Length}",
            _ => type.Name.Replace(".", "_").Replace("<", "_").Replace(">", "_")
        };
    }

    /// <summary>
    /// Generates inline tuple unwrapping code for a specific tuple value
    /// </summary>
    public void GenerateInlineTupleUnwrapping(CodeWriter writer, ITypeSymbol tupleType, string tupleVariableName, string targetArrayName)
    {
        if (tupleType is not INamedTypeSymbol { IsTupleType: true } namedTupleType)
        {
            return;
        }

        var elements = namedTupleType.TupleElements;
        
        for (int i = 0; i < elements.Length; i++)
        {
            var element = elements[i];
            var fieldName = element.IsExplicitlyNamedTupleElement ? element.Name : $"Item{i + 1}";
            
            if (element.Type is INamedTypeSymbol { IsTupleType: true })
            {
                // For nested tuples, create a temporary variable and recursively unwrap
                var tempVarName = $"nested{i}";
                writer.AppendLine($"var {tempVarName} = {tupleVariableName}.{fieldName};");
                GenerateInlineTupleUnwrapping(writer, element.Type, tempVarName, targetArrayName);
            }
            else
            {
                writer.AppendLine($"{targetArrayName}[{i}] = {tupleVariableName}.{fieldName};");
            }
        }
    }

    /// <summary>
    /// Checks if a type contains tuples that need unwrapping
    /// </summary>
    public bool ContainsTuples(ITypeSymbol type)
    {
        return type switch
        {
            INamedTypeSymbol { IsTupleType: true } => true,
            IArrayTypeSymbol array => ContainsTuples(array.ElementType),
            INamedTypeSymbol { IsGenericType: true } generic => generic.TypeArguments.Any(ContainsTuples),
            _ => false
        };
    }
}