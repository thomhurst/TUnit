using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Engine.Building;

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

        try
        {
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

            // Add method generic type arguments to ensure uniqueness for generic methods
            // e.g., GenericMethod<int> vs GenericMethod<string> should have different IDs
            if (combination.ResolvedMethodGenericArguments is { Length: > 0 })
            {
                vsb.Append('<');
                for (var i = 0; i < combination.ResolvedMethodGenericArguments.Length; i++)
                {
                    if (i > 0)
                    {
                        vsb.Append(',');
                    }
                    vsb.Append(combination.ResolvedMethodGenericArguments[i].FullName ?? combination.ResolvedMethodGenericArguments[i].Name);
                }
                vsb.Append('>');
            }

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
        finally
        {
            vsb.Dispose();
        }
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

        try
        {
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
        finally
        {
            vsb.Dispose();
        }
    }

    private static void WriteTypeNameWithGenerics(ref ValueStringBuilder vsb, Type type)
    {
        // Build the full type hierarchy including all containing types
        var typeHierarchy = new ValueListBuilder<string>([null, null, null, null]);
        var typeVsb = new ValueStringBuilder(stackalloc char[128]);
        try
        {
            var currentType = type;

            while (currentType != null)
            {
                if (currentType.IsGenericType)
                {
                    var name = currentType.Name;

                    var backtickIndex = name.IndexOf('`');
                    if (backtickIndex > 0)
                    {
                        typeVsb.Append(name.AsSpan(0, backtickIndex));
                    }
                    else
                    {
                        typeVsb.Append(name);
                    }

                    // Add the generic type arguments
                    var genericArgs = currentType.GetGenericArguments();
                    typeVsb.Append('<');
                    for (var i = 0; i < genericArgs.Length; i++)
                    {
                        if (i > 0)
                        {
                            typeVsb.Append(", ");
                        }
                        // Use the full name for generic arguments to ensure uniqueness
                        typeVsb.Append(genericArgs[i].FullName ?? genericArgs[i].Name);
                    }
                    typeVsb.Append('>');

                    typeHierarchy.Append(typeVsb.AsSpan().ToString());
                    typeVsb.Length = 0;
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
                if (i < typeHierarchy.Length - 1)
                {
                    vsb.Append('+');
                }
                vsb.Append(typeHierarchy[i]);
            }
        }
        finally
        {
            typeHierarchy.Dispose();
            typeVsb.Dispose();
        }
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
