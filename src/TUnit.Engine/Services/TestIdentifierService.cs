using TUnit.Core;
using TUnit.Core.Helpers;
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

    /// <summary>
    /// Generates a stable id for a deferred-enumeration placeholder (a single node that stands in for a
    /// data source whose rows are expanded at runtime). Deterministic from metadata alone so it is
    /// identical across the discovery build and the execution build (the IDE "run" filter carries this id).
    /// </summary>
    /// <remarks>
    /// Cannot collide with a real test id despite the shared <c>_Deferred</c> token: every real id from
    /// <see cref="GenerateTestId"/> has the form <c>...(ctorParams).{classIdx}.{classLoop}.Method(params).{methodIdx}.{methodLoop}.{repeat}</c>
    /// — i.e. the method name is always preceded by two numeric data-index segments and followed by three
    /// more. The placeholder id places the method name directly after the constructor params and ends in
    /// <c>_Deferred</c> with none of those numeric segments, so the two id shapes can never be equal (a
    /// method literally named <c>Foo_Deferred</c> still produces <c>...Foo_Deferred(params).i.j.k</c>, not
    /// <c>...Foo(params)_Deferred</c>). Two placeholders only match if their method+params match, which
    /// means the same method — impossible to declare twice.
    /// </remarks>
    public static string GenerateDeferredPlaceholderTestId(TestMetadata metadata)
    {
        var methodMetadata = metadata.MethodMetadata;
        var classMetadata = methodMetadata.Class;

        var constructorParameters = classMetadata.Parameters;
        var methodParameters = methodMetadata.Parameters;

        var vsb = new ValueStringBuilder(stackalloc char[256]);

        try
        {
            vsb.Append(methodMetadata.Class.Namespace);
            vsb.Append('.');
            WriteTypeNameWithGenerics(ref vsb, metadata.TestClassType);
            WriteTypeWithParameters(ref vsb, constructorParameters);
            vsb.Append('.');
            vsb.Append(metadata.TestMethodName);
            WriteTypeWithParameters(ref vsb, methodParameters);
            vsb.Append("_Deferred");

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
        // Collect the nested-type chain (inner -> outer) without allocating per-segment strings.
        var typeHierarchy = new ValueListBuilder<Type>([null!, null!, null!, null!]);
        try
        {
            var currentType = type;
            while (currentType != null)
            {
                typeHierarchy.Append(currentType);
                currentType = currentType.DeclaringType;
            }

            // Reverse to get outer-to-inner order (matches .NET Type.FullName convention for nested types).
            for (var i = typeHierarchy.Length - 1; i >= 0; i--)
            {
                if (i < typeHierarchy.Length - 1)
                {
                    vsb.Append('+');
                }
                TypeNameHelper.AppendTypeNameWithGenericArgs(ref vsb, typeHierarchy[i]);
            }
        }
        finally
        {
            typeHierarchy.Dispose();
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
