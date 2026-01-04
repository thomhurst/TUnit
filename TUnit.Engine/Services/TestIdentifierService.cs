using System.Text;
using TUnit.Core;
using TUnit.Engine.Building;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Services;

internal static class TestIdentifierService
{
    public static string GenerateTestId(TestMetadata metadata, TestBuilder.TestData combination)
    {
        var methodMetadata = metadata.MethodMetadata;
        var classMetadata = methodMetadata.Class;

        var constructorParameters = classMetadata.Parameters;
        var methodParameters = methodMetadata.Parameters;

        // Use ValueStringBuilder for efficient string concatenation
        var vsb = new ValueStringBuilder(stackalloc char[256]); // Pre-size for typical test ID length

        vsb.Append(methodMetadata.Class.Namespace);
        vsb.Append('.');
        WriteTypeNameWithGenerics(ref vsb, metadata.TestClassType);
        WriteTypeWithParameters(ref vsb, constructorParameters);
        vsb.Append('.');
        vsb.Append(combination.ClassDataSourceAttributeIndex);
        vsb.Append('.');
        vsb.Append(combination.ClassDataLoopIndex);
        vsb.Append('.');
        vsb.Append(metadata.TestMethodName);
        WriteTypeWithParameters(ref vsb, methodParameters);
        vsb.Append('.');
        vsb.Append(combination.MethodDataSourceAttributeIndex);
        vsb.Append('.');
        vsb.Append(combination.MethodDataLoopIndex);
        vsb.Append('.');
        vsb.Append(combination.RepeatIndex);

        // Add inheritance information to ensure uniqueness
        if (combination.InheritanceDepth > 0)
        {
            vsb.Append("_inherited");
            vsb.Append(combination.InheritanceDepth);
        }

        return vsb.ToString();
    }

    public static string GenerateFailedTestId(TestMetadata metadata)
    {
        // For backward compatibility, use default combination values
        return GenerateFailedTestId(metadata, new TestDataCombination());
    }

    public static string GenerateFailedTestId(TestMetadata metadata, TestDataCombination combination)
    {
        var methodMetadata = metadata.MethodMetadata;
        var classMetadata = methodMetadata.Class;

        var constructorParameters = classMetadata.Parameters;
        var methodParameters = methodMetadata.Parameters;

        // Use ValueStringBuilder for efficient string concatenation
        var vsb = new ValueStringBuilder(stackalloc char[256]); // Pre-size for typical test ID length

        vsb.Append(methodMetadata.Class.Namespace);
        vsb.Append('.');
        WriteTypeNameWithGenerics(ref vsb, metadata.TestClassType);
        WriteTypeWithParameters(ref vsb, constructorParameters);
        vsb.Append('.');
        vsb.Append(combination.ClassDataSourceIndex);
        vsb.Append('.');
        vsb.Append(combination.ClassLoopIndex);
        vsb.Append('.');
        vsb.Append(metadata.TestMethodName);
        WriteTypeWithParameters(ref vsb, methodParameters);
        vsb.Append('.');
        vsb.Append(combination.MethodDataSourceIndex);
        vsb.Append('.');
        vsb.Append(combination.MethodLoopIndex);
        vsb.Append('.');
        vsb.Append(combination.RepeatIndex);
        vsb.Append("_DataGenerationError");

        return vsb.ToString();
    }

    private static void WriteTypeNameWithGenerics(ref ValueStringBuilder vsb, Type type)
    {
        // Build the full type hierarchy including all containing types
        var typeHierarchy = new ValueListBuilder<string>([null, null, null, null]);
        var currentType = type;

        while (currentType != null)
        {
            if (currentType.IsGenericType)
            {
                var typeSb = new StringBuilder();
                var name = currentType.Name;

                var backtickIndex = name.IndexOf('`');
                if (backtickIndex > 0)
                {
#if NET6_0_OR_GREATER
                    typeSb.Append(name.AsSpan(0, backtickIndex));
#else
                    typeSb.Append(name.Substring(0, backtickIndex));
#endif
                }
                else
                {
                    typeSb.Append(name);
                }

                // Add the generic type arguments
                var genericArgs = currentType.GetGenericArguments();
                typeSb.Append('<');
                for (var i = 0; i < genericArgs.Length; i++)
                {
                    if (i > 0)
                    {
                        typeSb.Append(", ");
                    }
                    // Use the full name for generic arguments to ensure uniqueness
                    typeSb.Append(genericArgs[i].FullName ?? genericArgs[i].Name);
                }
                typeSb.Append('>');

                typeHierarchy.Append(typeSb.ToString());
            }
            else
            {
                typeHierarchy.Append(currentType.Name);
            }

            currentType = currentType.DeclaringType;
        }

        // Reverse to get outer-to-inner order
        // Append all types with + separator (matching .NET Type.FullName convention for nested types)
        for (var i = typeHierarchy.Length - 1; i >= 0; i--)
        {
            if (i > 0)
            {
                vsb.Append('+');
            }
            vsb.Append(typeHierarchy[i]);
        }
        typeHierarchy.Dispose();
    }

    private static void WriteTypeWithParameters(ref ValueStringBuilder vsb, ReadOnlySpan<ParameterMetadata> parameterTypes)
    {
        if (parameterTypes.Length == 0)
        {
            return;
        }

        vsb.Append('(');

        for (var i = 0; i < parameterTypes.Length; i++)
        {
            if (i > 0)
            {
                vsb.Append(", ");
            }

            vsb.Append(parameterTypes[i].Type.ToString());
        }

        vsb.Append(')');
    }
}
