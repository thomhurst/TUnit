using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record AsyncDataSourceGeneratorContainer(
    GeneratorAttributeSyntaxContext Context,
    AttributeData AttributeData,
    ArgumentsType ArgumentsType,
    ImmutableArray<ITypeSymbol> ParameterOrPropertyTypes,
    INamedTypeSymbol ClassMetadata,
    IMethodSymbol MethodMetadata,
    IPropertySymbol? Property,
    ImmutableArray<IParameterSymbol> Parameters,
    string[] GenericArguments) : ArgumentsContainer(ArgumentsType)
{
    public required string? PropertyName { get; init; }
    public required bool IsStronglyTyped { get; init; }

    public override void OpenScope(ICodeWriter sourceCodeWriter, ref int variableIndex)
    {
        if (ArgumentsType is ArgumentsType.Property)
        {
            // No scope as we don't allow enumerables for properties
            return;
        }

        var type = ArgumentsType == ArgumentsType.Method ? "TestParameters" : "ClassParameters";

        var dataGeneratorMetadataVariableName = $"{VariableNamePrefix}DataGeneratorMetadata";

        sourceCodeWriter.Append($"var {dataGeneratorMetadataVariableName} = new DataGeneratorMetadata");
        sourceCodeWriter.Append("{");
        sourceCodeWriter.Append($"Type = global::TUnit.Core.Enums.DataGeneratorType.{type},");
        sourceCodeWriter.Append("TestBuilderContext = testBuilderContextAccessor,");
        sourceCodeWriter.Append("TestInformation = testInformation,");

        sourceCodeWriter.Append("MembersToGenerate = ");
        SourceInformationWriter.GenerateMembers(sourceCodeWriter, Context, ClassMetadata, Parameters, null, ArgumentsType);

        sourceCodeWriter.Append("TestSessionId = sessionId,");
        sourceCodeWriter.Append("TestClassInstance = classInstance,");
        sourceCodeWriter.Append("ClassInstanceArguments = classInstanceArguments,");
        sourceCodeWriter.Append("};");

        var arrayVariableName = $"{VariableNamePrefix}GeneratedDataArray";
        var generatedDataVariableName = $"{VariableNamePrefix}GeneratedData";

        var dataAttr = GenerateDataAttributeVariable("var", string.Empty,
            ref variableIndex);

        sourceCodeWriter.Append($"{dataAttr.Type} {dataAttr.Name} = ");
        AttributeWriter.WriteAttribute(sourceCodeWriter, Context, AttributeData);
        sourceCodeWriter.Append(";");
        sourceCodeWriter.AppendLine();

        sourceCodeWriter.AppendLine();

        sourceCodeWriter.Append($"testBuilderContext.DataAttributes.Add({dataAttr.Name});");
        sourceCodeWriter.AppendLine();

        sourceCodeWriter.Append($"var {arrayVariableName} = ((global::TUnit.Core.IAsyncDataSourceGeneratorAttribute){dataAttr.Name}).GenerateAsync({dataGeneratorMetadataVariableName});");
        sourceCodeWriter.AppendLine();
        sourceCodeWriter.Append($"await foreach (var {generatedDataVariableName}Accessor in {arrayVariableName})");
        sourceCodeWriter.Append("{");

        if (ArgumentsType == ArgumentsType.ClassConstructor)
        {
            sourceCodeWriter.Append($"{CodeGenerators.VariableNames.ClassDataIndex}++;");
        }

        if (ArgumentsType == ArgumentsType.Method)
        {
            sourceCodeWriter.Append($"{CodeGenerators.VariableNames.TestMethodDataIndex}++;");
        }
    }

    public override void WriteVariableAssignments(ICodeWriter sourceCodeWriter, ref int variableIndex)
    {
        var type = ArgumentsType switch
        {
            ArgumentsType.Property => "Property",
            ArgumentsType.ClassConstructor => "ClassParameters",
            _ => "TestParameters"
        };

        if (Property is not null)
        {
            var attr = GenerateDataAttributeVariable("var",
                string.Empty,
                ref variableIndex);

            sourceCodeWriter.Append($"{attr.Type} {attr.Name} = ");
            AttributeWriter.WriteAttribute(sourceCodeWriter, Context, AttributeData);
            sourceCodeWriter.Append(";");
            sourceCodeWriter.AppendLine();

            sourceCodeWriter.Append($"testBuilderContext.DataAttributes.Add({attr.Name});");
            sourceCodeWriter.AppendLine();

            // Use the property type instead of var to avoid CS0815
            var propertyType = Property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var dataSourceVariable = GenerateVariable(propertyType, string.Empty, ref variableIndex);

            sourceCodeWriter.Append($"{dataSourceVariable.Type} {dataSourceVariable.Name} = ");

            sourceCodeWriter.Append(GetPropertyAssignmentFromAsyncDataSourceGeneratorAttribute(attr.Name, Context, ClassMetadata, Property, false));
            sourceCodeWriter.AppendLine();
            
            // Initialize the property value if it implements IAsyncInitializer
            sourceCodeWriter.Append($"if ({dataSourceVariable.Name} is global::TUnit.Core.Interfaces.IAsyncInitializer)");
            sourceCodeWriter.Append("{");
            sourceCodeWriter.Append($"await global::TUnit.Core.ObjectInitializer.InitializeAsync({dataSourceVariable.Name});");
            sourceCodeWriter.Append("}");
            return;
        }

        var generatedDataVariableName = $"{VariableNamePrefix}GeneratedData";

        sourceCodeWriter.Append($"var {generatedDataVariableName} = await {generatedDataVariableName}Accessor();");

        if (GenericArguments.Length == 0)
        {
            for (var i = 0; i < ParameterOrPropertyTypes.Length; i++)
            {
                var refIndex = i;

                if (IsStronglyTyped)
                {
                    sourceCodeWriter.Append(GenerateVariable(ParameterOrPropertyTypes[i].GloballyQualified(), $"({ParameterOrPropertyTypes[i].GloballyQualified()}){generatedDataVariableName}[{i}]", ref refIndex).ToString());
                }
                else
                {
                    sourceCodeWriter.Append(GenerateVariable(ParameterOrPropertyTypes[i].GloballyQualified(), $"global::TUnit.Core.Helpers.CastHelper.Cast<{ParameterOrPropertyTypes[i].GloballyQualified()}>({generatedDataVariableName}[{i}])", ref refIndex).ToString());
                }
            }
        }
        else if (GenericArguments.Length > 1)
        {
            for (var i = 0; i < GenericArguments.Length; i++)
            {
                var refIndex = i;
                sourceCodeWriter.Append(GenerateVariable(GenericArguments[i], $"global::TUnit.Core.Helpers.CastHelper.Cast<{GenericArguments[i]}>({generatedDataVariableName}[{i}])", ref refIndex).ToString());
            }

            sourceCodeWriter.AppendLine();
        }
        else
        {
            // For single generic argument, extract the first element
            if (GenericArguments.Length == 1)
            {
                var refIndex = 0;
                sourceCodeWriter.Append(GenerateVariable(GenericArguments[0], $"global::TUnit.Core.Helpers.CastHelper.Cast<{GenericArguments[0]}>({generatedDataVariableName}[0])", ref refIndex).ToString());
            }
            else
            {
                DataVariables.Add(new Variable
                {
                    Type = "var",
                    Name = $"{generatedDataVariableName}",
                    Value = string.Empty
                });
            }
        }
    }

    public static string GetPropertyAssignmentFromAsyncDataSourceGeneratorAttribute(string attributeVariableName, GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol, IPropertySymbol property, bool isNested)
    {
        var sourceCodeWriter = new CodeWriter("", includeHeader: false);

        var propertyType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isReferenceType = property.Type.IsReferenceType;
        var isNullable = property.Type.NullableAnnotation == NullableAnnotation.Annotated;

        sourceCodeWriter.Append("await (async () => {");
        sourceCodeWriter.Append($"var enumerator = ((global::TUnit.Core.IAsyncDataSourceGeneratorAttribute){attributeVariableName}).GenerateAsync(");
        WriteDataGeneratorMetadataProperty(sourceCodeWriter, context, namedTypeSymbol, property);
        sourceCodeWriter.Append(").GetAsyncEnumerator();");
        sourceCodeWriter.AppendLine();
        sourceCodeWriter.Append("try");
        sourceCodeWriter.Append("{");
        sourceCodeWriter.Append("if (await enumerator.MoveNextAsync())");
        sourceCodeWriter.Append("{");
        sourceCodeWriter.Append("var result = await enumerator.Current();");
        sourceCodeWriter.Append($"return ({propertyType})(result?.ElementAtOrDefault(0)");

        if (isReferenceType && !isNullable)
        {
            sourceCodeWriter.Append(")!; ");
        }
        else if (property.Type.IsValueType && !isNullable)
        {
            sourceCodeWriter.Append($" ?? default({propertyType}));");
        }
        else
        {
            sourceCodeWriter.Append(");");
        }

        sourceCodeWriter.Append("} ");
        sourceCodeWriter.Append($"return default({propertyType});");
        sourceCodeWriter.Append("}");
        sourceCodeWriter.Append("finally");
        sourceCodeWriter.Append("{");
        sourceCodeWriter.Append("await enumerator.DisposeAsync();");
        sourceCodeWriter.Append("}");
        sourceCodeWriter.Append("})()");
        if (isNested)
        {
            sourceCodeWriter.Append(",");
        }
        else
        {
            sourceCodeWriter.Append(";");
        }
        sourceCodeWriter.AppendLine();

        return sourceCodeWriter.ToString();
    }

    public static void WriteDataGeneratorMetadataProperty(ICodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol, IPropertySymbol property)
    {
        sourceCodeWriter.Append("new DataGeneratorMetadata");
        sourceCodeWriter.AppendLine();
        sourceCodeWriter.Append("{");
        sourceCodeWriter.Append($"Type = global::TUnit.Core.Enums.DataGeneratorType.Property,");
        sourceCodeWriter.Append("TestBuilderContext = testBuilderContextAccessor,");
        sourceCodeWriter.Append("TestInformation = testInformation,");

        sourceCodeWriter.Append("MembersToGenerate = ");
        SourceInformationWriter.GenerateMembers(sourceCodeWriter, context, namedTypeSymbol, ImmutableArray<IParameterSymbol>.Empty, property, ArgumentsType.Property);

        sourceCodeWriter.Append("TestSessionId = sessionId,");
        sourceCodeWriter.Append("TestClassInstance = classInstance,");
        sourceCodeWriter.Append("ClassInstanceArguments = classInstanceArguments,");
        sourceCodeWriter.Append("}");
    }

    public override void CloseScope(ICodeWriter sourceCodeWriter)
    {
        if (ArgumentsType is ArgumentsType.Property)
        {
            return;
        }

        sourceCodeWriter.Append("}");
    }

    public override string[] GetArgumentTypes()
    {
        return GenericArguments;
    }
}
