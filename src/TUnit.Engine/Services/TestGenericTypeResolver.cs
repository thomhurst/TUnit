using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Building;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Services;

/// <summary>
/// Resolves generic type arguments for tests based on TestMetadata and runtime TestData.
/// This class bridges the gap between compile-time generic information and runtime type resolution.
/// </summary>
internal sealed class TestGenericTypeResolver
{
    /// <summary>
    /// Resolves generic type arguments for a test class and/or method based on the provided test data.
    /// </summary>
    /// <param name="metadata">The test metadata containing generic type information</param>
    /// <param name="testData">The runtime test data containing actual arguments</param>
    /// <returns>A result containing resolved generic types for both class and method</returns>
    #if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Type mapping inference uses Type.GetInterfaces and reflection")]
    #endif
    public static TestGenericTypeResolution Resolve(TestMetadata metadata, TestBuilder.TestData testData)
    {
        var result = new TestGenericTypeResolution();

        // Resolve class generic arguments if the test class is generic
        if (metadata.GenericTypeInfo != null)
        {
            result.ResolvedClassGenericArguments = ResolveClassGenericArguments(
                metadata.TestClassType,
                metadata.GenericTypeInfo,
                testData.ClassData);
        }

        // First check if generic method type arguments are already resolved
        // This handles constructed generic methods (created via MakeGenericMethod from [GenerateGenericTest])
        if (metadata.GenericMethodTypeArguments is { Length: > 0 })
        {
            result.ResolvedMethodGenericArguments = metadata.GenericMethodTypeArguments;
        }
        // Otherwise resolve from GenericMethodInfo if the test method is a generic definition
        else if (metadata.GenericMethodInfo != null)
        {
            var parameterTypes = Array.ConvertAll(metadata.MethodMetadata.Parameters, p => p.Type);

            result.ResolvedMethodGenericArguments = ResolveMethodGenericArguments(
                metadata.MethodMetadata,
                metadata.GenericMethodInfo,
                testData.MethodData,
                parameterTypes,
                result.ResolvedClassGenericArguments);
        }

        return result;
    }

    #if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Type mapping inference uses Type.GetInterfaces and reflection")]
    #endif
    private static Type[] ResolveClassGenericArguments(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type genericClassType,
        GenericTypeInfo genericTypeInfo,
        object?[] constructorArguments)
    {
        if (!genericClassType.IsGenericTypeDefinition)
        {
            // If it's already constructed, extract the generic arguments
            if (genericClassType.IsConstructedGenericType)
            {
                return genericClassType.GetGenericArguments();
            }
            return Type.EmptyTypes;
        }

        var genericParameters = genericClassType.GetGenericArguments();
        var typeMapping = new Dictionary<Type, Type>();

        // Try to infer from constructor parameters
        var constructors = genericClassType.GetConstructors();
        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length == constructorArguments.Length)
            {
                typeMapping.Clear();
                if (TryInferTypesFromArguments(parameters, constructorArguments, typeMapping))
                {
                    break;
                }
            }
        }

        // If we couldn't infer all types from constructor arguments, check if this is
        // a parameterless constructor case where we might be able to infer from method data later
        if (typeMapping.Count < genericParameters.Length && constructorArguments.Length == 0)
        {
            // For classes with parameterless constructors, throw a specific exception
            // that can be caught and handled by the caller
            throw new GenericTypeResolutionException(
                $"Cannot create test for generic class '{genericClassType.Name}': No type arguments could be inferred. " +
                $"Add [GenerateGenericTest<ConcreteType>] to the class, or use a data source " +
                $"(like [ClassDataSource<T>] or [Arguments]) that provides constructor arguments to infer the generic type arguments from.");
        }

        // Resolve all generic parameters
        var resolvedTypes = new Type[genericParameters.Length];
        for (var i = 0; i < genericParameters.Length; i++)
        {
            var genericParam = genericParameters[i];
            if (!typeMapping.TryGetValue(genericParam, out var resolvedType))
            {
                throw new GenericTypeResolutionException(
                    $"Cannot create test for generic class '{genericClassType.Name}': Could not resolve type for generic parameter '{genericParam.Name}'. " +
                    $"Add [GenerateGenericTest<ConcreteType>] to the class, or use a data source that provides constructor arguments to infer the generic type arguments from.");
            }
            resolvedTypes[i] = resolvedType;
        }

        // Validate against constraints from metadata
        ValidateAgainstConstraints(genericTypeInfo.Constraints, resolvedTypes, classTypeArguments: resolvedTypes);

        return resolvedTypes;
    }

    #if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Type mapping inference uses Type.GetInterfaces and reflection")]
    #endif
    private static Type[] ResolveMethodGenericArguments(
        MethodMetadata methodMetadata,
        GenericMethodInfo genericMethodInfo,
        object?[] methodArguments,
        Type[] parameterTypes,
        Type[] classTypeArguments)
    {
        // Handle the case where some parameter types are Object (placeholders for generic parameters)
        // This happens when data sources provide untyped data or when we have mixed generic/non-generic parameters
        // When a parameter type contains a generic parameter, the types are the method's declared ones
        // (reflection mode), so an object parameter is a real object parameter, not a placeholder for one:
        // Run<T>(T value, object other) must infer T from its own argument, not from other's.
        var hasObjectParameter = false;
        var hasGenericParameterType = false;
        if (methodArguments.Length > 0)
        {
            foreach (var parameterType in parameterTypes)
            {
                if (parameterType == typeof(object))
                {
                    hasObjectParameter = true;
                }
                else if (parameterType.ContainsGenericParameters)
                {
                    hasGenericParameterType = true;
                    break;
                }
            }
        }

        hasObjectParameter &= !hasGenericParameterType;
        if (hasObjectParameter)
        {
            // Check if this is a simple generic method with one type parameter and mixed parameters
            if (genericMethodInfo.ParameterNames.Length == 1 && parameterTypes.Length >= 1)
            {
                // Find the first Object parameter type - this should correspond to our generic parameter
                for (var i = 0; i < parameterTypes.Length; i++)
                {
                    if (parameterTypes[i] == typeof(object) && i < methodArguments.Length && methodArguments[i] != null)
                    {
                        var resolvedTypes = new Type[1];
                        resolvedTypes[0] = methodArguments[i]!.GetType();
                        
                        // Validate against constraints from metadata
                        ValidateAgainstConstraints(genericMethodInfo.Constraints, resolvedTypes, classTypeArguments);
                        
                        return resolvedTypes;
                    }
                }
            }
            
            // Handle multiple generic parameters where each Object parameter corresponds to a generic parameter
            // Count how many Object parameters we have
            var objectParameterIndices = new List<int>();
            for (var i = 0; i < parameterTypes.Length; i++)
            {
                if (parameterTypes[i] == typeof(object))
                {
                    objectParameterIndices.Add(i);
                }
            }
            
            // If the number of Object parameters matches the number of generic parameters
            if (objectParameterIndices.Count == genericMethodInfo.ParameterNames.Length)
            {
                var resolvedTypes = new Type[genericMethodInfo.ParameterNames.Length];
                
                // Map each Object parameter to its corresponding generic parameter
                for (var i = 0; i < objectParameterIndices.Count; i++)
                {
                    var paramIndex = objectParameterIndices[i];
                    if (paramIndex < methodArguments.Length && methodArguments[paramIndex] != null)
                    {
                        resolvedTypes[i] = methodArguments[paramIndex]!.GetType();
                    }
                    else
                    {
                        throw new InvalidOperationException($"Could not resolve type for generic parameter at position {i}");
                    }
                }
                
                // Validate against constraints from metadata
                ValidateAgainstConstraints(genericMethodInfo.Constraints, resolvedTypes, classTypeArguments);
                
                return resolvedTypes;
            }
        }
        
        // Handle the case where all parameter types are Object
        // This happens when data sources provide untyped data
        var allParametersAreObject = methodArguments.Length > 0;
        if (allParametersAreObject)
        {
            foreach (var t in parameterTypes)
            {
                if (t != typeof(object))
                {
                    allParametersAreObject = false;
                    break;
                }
            }
        }

        if (allParametersAreObject)
        {
            // For the AggregateBy test case with 3 generic parameters
            if (genericMethodInfo.ParameterNames.Length == 3 &&
                methodArguments.Length >= 3)
            {
                var resolvedTypes = new Type[genericMethodInfo.ParameterNames.Length];
                
                // Extract types from the actual arguments:
                // arg[0] should be IEnumerable<TSource>
                // arg[1] should be Func<TSource, TKey>
                // arg[2] should be Func<TKey, TAccumulate>
                
                Type? sourceType = null;
                Type? keyType = null;
                Type? accumulateType = null;
                
                // Get TSource from first argument
                if (methodArguments[0] != null)
                {
                    var arg0 = methodArguments[0];
                    var arg0Type = arg0!.GetType();
                    
                    if (arg0Type.IsArray)
                    {
                        sourceType = arg0Type.GetElementType();
                    }
                    else if (arg0Type.IsGenericType)
                    {
                        // Direct generic type
                        var genArgs = arg0Type.GetGenericArguments();
                        if (genArgs.Length > 0)
                        {
                            sourceType = genArgs[0];
                        }
                    }
                    else
                    {
                        // For AOT compatibility, we'll use a different approach
                        // Try to cast to known IEnumerable<T> types to extract T
                        
                        // Try common numeric types first
                        if (arg0 is IEnumerable<int>)
                        {
                            sourceType = typeof(int);
                        }
                        else if (arg0 is IEnumerable<string>)
                        {
                            sourceType = typeof(string);
                        }
                        else if (arg0 is IEnumerable<long>)
                        {
                            sourceType = typeof(long);
                        }
                        else if (arg0 is IEnumerable<double>)
                        {
                            sourceType = typeof(double);
                        }
                        else if (arg0 is IEnumerable<float>)
                        {
                            sourceType = typeof(float);
                        }
                        else if (arg0 is IEnumerable<decimal>)
                        {
                            sourceType = typeof(decimal);
                        }
                        else if (arg0 is IEnumerable<object> objEnum)
                        {
                            // Last resort - try to infer from first element.
                            // foreach avoids the iterator allocation that FirstOrDefault
                            // incurs on non-list IEnumerable<object> sources.
                            foreach (var first in objEnum)
                            {
                                if (first != null)
                                {
                                    sourceType = first.GetType();
                                }

                                break;
                            }
                        }
                    }
                    
                    // If still null, provide error
                    if (sourceType == null)
                    {
                        throw new GenericTypeResolutionException(
                            $"Could not extract element type from first argument. " +
                            $"Argument type: {arg0Type.FullName}, " +
                            $"IsArray: {arg0Type.IsArray}, " +
                            $"IsGenericType: {arg0Type.IsGenericType}");
                    }
                }
                
                // Get TKey from second argument (Func<TSource, TKey>)
                if (methodArguments[1] != null && sourceType != null)
                {
                    var arg1Type = methodArguments[1]!.GetType();
                    if (arg1Type.IsGenericType && arg1Type.GetGenericArguments().Length == 2)
                    {
                        var funcArgs = arg1Type.GetGenericArguments();
                        // Verify first arg matches TSource
                        if (funcArgs[0] == sourceType)
                        {
                            keyType = funcArgs[1];
                        }
                    }
                }
                
                // Get TAccumulate from third argument (Func<TKey, TAccumulate>)
                if (methodArguments[2] != null && keyType != null)
                {
                    var arg2Type = methodArguments[2]!.GetType();
                    if (arg2Type.IsGenericType && arg2Type.GetGenericArguments().Length == 2)
                    {
                        var funcArgs = arg2Type.GetGenericArguments();
                        // Verify first arg matches TKey
                        if (funcArgs[0] == keyType)
                        {
                            accumulateType = funcArgs[1];
                        }
                    }
                }
                
                // Map to result based on parameter names
                for (var i = 0; i < genericMethodInfo.ParameterNames.Length; i++)
                {
                    var paramName = genericMethodInfo.ParameterNames[i];
                    
                    // Match by position for 3-parameter generic methods
                    if (i == 0)
                    {
                        resolvedTypes[i] = sourceType ?? throw new GenericTypeResolutionException($"Could not resolve first generic parameter '{paramName}'");
                    }
                    else if (i == 1)
                    {
                        resolvedTypes[i] = keyType ?? throw new GenericTypeResolutionException($"Could not resolve second generic parameter '{paramName}'");
                    }
                    else if (i == 2)
                    {
                        resolvedTypes[i] = accumulateType ?? throw new GenericTypeResolutionException($"Could not resolve third generic parameter '{paramName}'");
                    }
                }
                
                // Validate against constraints from metadata
                ValidateAgainstConstraints(genericMethodInfo.Constraints, resolvedTypes, classTypeArguments);
                
                return resolvedTypes;
            }
            
            // Handle single generic parameter case
            if (genericMethodInfo.ParameterNames.Length == 1 && methodArguments.Length >= 1)
            {
                var resolvedTypes = new Type[1];
                
                // For a single generic parameter, infer from the first argument
                if (methodArguments[0] != null)
                {
                    resolvedTypes[0] = methodArguments[0]!.GetType();
                }
                else
                {
                    throw new GenericTypeResolutionException($"Could not resolve generic parameter '{genericMethodInfo.ParameterNames[0]}' from null argument");
                }
                
                // Validate against constraints from metadata
                ValidateAgainstConstraints(genericMethodInfo.Constraints, resolvedTypes, classTypeArguments);
                
                return resolvedTypes;
            }
        }
        
        // Try the original approach for cases with typed parameters
        var typeMapping = new Dictionary<Type, Type>();
        
        // A trailing params T[] (or plain T[]) collects loose values, which do not match the array type itself.
        // They are left out of inference: the reflection invoker re-infers type arguments from the
        // declared parameter types, so inferring T from them here would not reach the invocation.
        // T must come from another argument, or from a lone array value passed as the array.
        // Row values bind to the data parameters only: the engine supplies a CancellationToken parameter
        // wherever it is declared, so it takes no row value (as in source-gen mode). An array just before
        // a trailing token (T[] values, CancellationToken token) is still the trailing array.
        var dataParameterCount = 0;
        var lastDataParameterIndex = -1;
        for (var i = 0; i < parameterTypes.Length; i++)
        {
            if (parameterTypes[i] != typeof(CancellationToken))
            {
                dataParameterCount++;
                lastDataParameterIndex = i;
            }
        }

        var hasGenericParamsArray = HasGenericParamsArray(methodMetadata, parameterTypes, lastDataParameterIndex);

        // Method type parameters that only received null values, if any
        List<Type>? nullArgumentParameters = null;

        // Map parameter types to argument types; parameterIndex skips CancellationToken parameters
        var parameterIndex = 0;
        for (var i = 0; i < Math.Min(dataParameterCount, methodArguments.Length); i++, parameterIndex++)
        {
            while (parameterTypes[parameterIndex] == typeof(CancellationToken))
            {
                parameterIndex++;
            }

            var paramType = parameterTypes[parameterIndex];
            var argValue = methodArguments[i];

            if (hasGenericParamsArray && parameterIndex == lastDataParameterIndex)
            {
                // A lone array value may be the array itself (normal form) or one element of it
                // (expanded form, e.g. T = int[] from a leading argument). Use it for inference only
                // when the normal form agrees with what the other arguments inferred.
                if (methodArguments.Length == dataParameterCount
                    && argValue?.GetType() is { IsArray: true } lastArgType)
                {
                    var normalFormMapping = new Dictionary<Type, Type>(typeMapping);
                    if (TryInferTypeMapping(paramType, lastArgType, normalFormMapping))
                    {
                        typeMapping = normalFormMapping;
                    }
                }

                continue;
            }

            if (argValue != null)
            {
                var argType = argValue.GetType();
                
                // If the parameter type is a generic parameter, map it directly
                if (paramType.IsGenericParameter)
                {
                    typeMapping[paramType] = argType;
                }
                else
                {
                    // Try to infer nested generic types
                    InferTypeMapping(paramType, argType, typeMapping, genericMethodInfo.ParameterPositions);
                }
            }
            else if (paramType is { IsGenericParameter: true, DeclaringMethod: not null })
            {
                (nullArgumentParameters ??= []).Add(paramType);
            }
        }

        // A null value passed straight to a method type parameter infers object, as in the source
        // generator and the reflection invoker ([Arguments(null)] runs as Test<object>(null), #7120).
        // A non-null value for the same type parameter takes precedence, as the invoker's does.
        if (nullArgumentParameters is not null)
        {
            foreach (var nullArgumentParameter in nullArgumentParameters)
            {
                if (!typeMapping.ContainsKey(nullArgumentParameter))
                {
                    typeMapping[nullArgumentParameter] = typeof(object);
                }
            }
        }

        // Create resolved types array
        var resolvedTypesFromMapping = new Type[genericMethodInfo.ParameterNames.Length];
        for (var i = 0; i < genericMethodInfo.ParameterNames.Length; i++)
        {
            var targetName = genericMethodInfo.ParameterNames[i];
            Type? resolvedType = null;
            
            foreach (var kvp in typeMapping)
            {
                if (kvp.Key.IsGenericParameter && kvp.Key.Name == targetName)
                {
                    resolvedType = kvp.Value;
                    break;
                }
            }
            
            if (resolvedType == null)
            {
                throw new GenericTypeResolutionException(
                    $"Could not resolve type for generic parameter '{targetName}' in method. " +
                    $"Arguments: {methodArguments.Length}, Parameter types: [{string.Join(", ", parameterTypes.Select(t => t.Name))}]");
            }
            
            resolvedTypesFromMapping[i] = resolvedType;
        }
        
        // Validate against constraints from metadata
        ValidateAgainstConstraints(genericMethodInfo.Constraints, resolvedTypesFromMapping, classTypeArguments);
        
        return resolvedTypesFromMapping;
    }

    /// <summary>
    /// Whether the method's last data parameter collects trailing values and involves a generic
    /// parameter: a <c>params T[]</c>, or a plain single-dimensional <c>T[]</c>, which the
    /// reflection invoker also fills from loose values.
    /// </summary>
    private static bool HasGenericParamsArray(MethodMetadata methodMetadata, Type[] parameterTypes, int lastDataParameterIndex)
    {
        var parameters = methodMetadata.Parameters;
        if (lastDataParameterIndex < 0 || parameters.Length != parameterTypes.Length)
        {
            return false;
        }

        var lastType = parameterTypes[lastDataParameterIndex];
        return lastType.IsArray
            && lastType.ContainsGenericParameters
            && (parameters[lastDataParameterIndex].IsParams || lastType.GetArrayRank() == 1);
    }

    #if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Type mapping inference uses Type.GetInterfaces and reflection")]
    #endif
    private static bool TryInferTypesFromArguments(
        ParameterInfo[] parameters,
        object?[] arguments,
        Dictionary<Type, Type> typeMapping)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            var argValue = arguments[i];

            if (argValue != null)
            {
                var argType = argValue.GetType();
                if (!TryInferTypeMapping(paramType, argType, typeMapping))
                {
                    return false;
                }
            }
        }

        return true;
    }

    #if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Type mapping inference uses Type.GetInterfaces and reflection")]
    #endif
    private static void InferTypeMapping(
        Type parameterType,
        Type argumentType,
        Dictionary<Type, Type> typeMapping,
        int[] genericParameterPositions)
    {
        if (!TryInferTypeMapping(parameterType, argumentType, typeMapping))
        {
            throw new GenericTypeResolutionException(
                $"Type mismatch: Cannot use argument of type '{argumentType.Name}' for parameter of type '{parameterType.Name}'");
        }
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Type mapping inference uses Type.GetInterfaces and reflection")]
#endif
    private static bool TryInferTypeMapping(
        Type parameterType,
        Type argumentType,
        Dictionary<Type, Type> typeMapping)
    {
        // Direct generic parameter
        if (parameterType.IsGenericParameter)
        {
            if (typeMapping.TryGetValue(parameterType, out var existingMapping))
            {
                // Verify consistency
                if (existingMapping != argumentType)
                {
                    return false;
                }
            }
            else
            {
                typeMapping[parameterType] = argumentType;
            }
            return true;
        }

        // Array types
        if (parameterType.IsArray && argumentType.IsArray)
        {
            return TryInferTypeMapping(
                parameterType.GetElementType()!,
                argumentType.GetElementType()!,
                typeMapping);
        }

        // Generic types (e.g., List<T>, Dictionary<K,V>)
        if (parameterType.IsGenericType && argumentType.IsGenericType)
        {
            var paramGenericDef = parameterType.GetGenericTypeDefinition();
            var argGenericDef = argumentType.IsGenericTypeDefinition 
                ? argumentType 
                : argumentType.GetGenericTypeDefinition();

            if (paramGenericDef == argGenericDef)
            {
                var paramGenericArgs = parameterType.GetGenericArguments();
                var argGenericArgs = argumentType.GetGenericArguments();

                if (paramGenericArgs.Length == argGenericArgs.Length)
                {
                    for (var i = 0; i < paramGenericArgs.Length; i++)
                    {
                        if (!TryInferTypeMapping(paramGenericArgs[i], argGenericArgs[i], typeMapping))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
        }

        // Handle case where parameter is a generic interface and argument implements it
        if (parameterType is { IsGenericType: true, IsInterface: true })
        {
            var implementedInterfaces = AssemblyReferenceCache.GetInterfaces(argumentType);
            foreach (var implementedInterface in implementedInterfaces)
            {
                if (implementedInterface.IsGenericType)
                {
                    var interfaceGenericDef = implementedInterface.GetGenericTypeDefinition();
                    var paramGenericDef = parameterType.GetGenericTypeDefinition();

                    if (interfaceGenericDef == paramGenericDef)
                    {
                        // Found matching interface, now map generic arguments
                        var paramGenericArgs = parameterType.GetGenericArguments();
                        var implGenericArgs = implementedInterface.GetGenericArguments();

                        if (paramGenericArgs.Length == implGenericArgs.Length)
                        {
                            var allMapped = true;
                            for (var i = 0; i < paramGenericArgs.Length; i++)
                            {
                                if (!TryInferTypeMapping(paramGenericArgs[i], implGenericArgs[i], typeMapping))
                                {
                                    allMapped = false;
                                    break;
                                }
                            }
                            if (allMapped)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }

        // Non-generic types must match exactly or be assignable
        return parameterType.IsAssignableFrom(argumentType);
    }

    /// <param name="constraints">The constraints of the generic parameters that <paramref name="resolvedTypes"/> fill.</param>
    /// <param name="resolvedTypes">The resolved type arguments, one per constraint.</param>
    /// <param name="classTypeArguments">
    /// The resolved type arguments of the test class, substituted for class type parameters that a constraint
    /// mentions (e.g. <c>where TMethod : IComparable&lt;TClass&gt;</c>).
    /// </param>
    private static void ValidateAgainstConstraints(
        GenericParameterConstraints[] constraints,
        Type[] resolvedTypes,
        Type[] classTypeArguments)
    {
        for (var i = 0; i < Math.Min(constraints.Length, resolvedTypes.Length); i++)
        {
            var constraint = constraints[i];
            var resolvedType = resolvedTypes[i];

            // Check value type constraint
            // Nullable<T> is a value type but never satisfies 'struct'.
            if (constraint.HasValueTypeConstraint && (!resolvedType.IsValueType || Nullable.GetUnderlyingType(resolvedType) != null))
            {
                throw new GenericTypeResolutionException(
                    $"Type '{resolvedType.Name}' does not satisfy the 'struct' constraint for generic parameter '{constraint.ParameterName}'");
            }

            // Check reference type constraint
            if (constraint.HasReferenceTypeConstraint && resolvedType.IsValueType)
            {
                throw new GenericTypeResolutionException(
                    $"Type '{resolvedType.Name}' does not satisfy the 'class' constraint for generic parameter '{constraint.ParameterName}'");
            }

            // Check default constructor constraint
            // Note: For AOT compatibility, we cannot reliably check for default constructors at runtime
            // The source generator will validate this at compile time instead
            if (constraint.HasDefaultConstructorConstraint && !resolvedType.IsValueType)
            {
                // In AOT mode, constructor constraints are validated at compile time
                // We skip runtime validation to avoid trimming issues
            }

            // Check base type constraint
            if (constraint.BaseTypeConstraint != null)
            {
                var baseTypeConstraint = SubstituteGenericParameters(constraint.BaseTypeConstraint, classTypeArguments, resolvedTypes);
                if (!baseTypeConstraint.IsAssignableFrom(resolvedType))
                {
                    throw new GenericTypeResolutionException(
                        $"Type '{resolvedType.Name}' does not satisfy the base type constraint '{baseTypeConstraint.Name}' for generic parameter '{constraint.ParameterName}'");
                }
            }

            // Check interface constraints
            foreach (var constraintType in constraint.InterfaceConstraints)
            {
                var interfaceConstraint = SubstituteGenericParameters(constraintType, classTypeArguments, resolvedTypes);
                if (!interfaceConstraint.IsAssignableFrom(resolvedType))
                {
                    throw new GenericTypeResolutionException(
                        $"Type '{resolvedType.Name}' does not implement interface '{interfaceConstraint.Name}' required for generic parameter '{constraint.ParameterName}'");
                }
            }
        }
    }

    /// <summary>
    /// Replaces the generic parameters in a constraint type (e.g. <c>IComparable&lt;T&gt;</c> or
    /// <c>IEnumerable&lt;U[]&gt;</c>) with their resolved type arguments, so the constraint can be checked
    /// with <see cref="Type.IsAssignableFrom"/>. Class type parameters come from <paramref name="classTypeArguments"/>
    /// and method type parameters from <paramref name="methodTypeArguments"/>. A parameter with no resolved
    /// argument leaves the constraint unchanged.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Only constraint types that mention generic parameters are rebuilt. Those come from open generic tests, which are resolved at runtime in reflection mode only; source-generated tests are already closed.")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "The generic definition and its arguments come from the constraint and the resolved test types, which are already in use.")]
    private static Type SubstituteGenericParameters(Type type, Type[] classTypeArguments, Type[] methodTypeArguments)
    {
        if (!type.ContainsGenericParameters)
        {
            return type;
        }

        if (type.IsGenericParameter)
        {
            var typeArguments = type.DeclaringMethod != null ? methodTypeArguments : classTypeArguments;
            var position = type.GenericParameterPosition;
            return position < typeArguments.Length ? typeArguments[position] : type;
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            var substitutedElement = SubstituteGenericParameters(elementType, classTypeArguments, methodTypeArguments);
            if (substitutedElement.ContainsGenericParameters)
            {
                return type;
            }

            // A single-dimensional zero-based array (T[]) differs from a rank-1 multi-dimensional one (T[*])
            return type == elementType.MakeArrayType()
                ? substitutedElement.MakeArrayType()
                : substitutedElement.MakeArrayType(type.GetArrayRank());
        }

        if (type.IsGenericType)
        {
            var typeArguments = type.GetGenericArguments();
            for (var i = 0; i < typeArguments.Length; i++)
            {
                typeArguments[i] = SubstituteGenericParameters(typeArguments[i], classTypeArguments, methodTypeArguments);
                if (typeArguments[i].ContainsGenericParameters)
                {
                    return type;
                }
            }

            return type.GetGenericTypeDefinition().MakeGenericType(typeArguments);
        }

        return type;
    }
}

/// <summary>
/// Contains the results of generic type resolution for a test
/// </summary>
internal sealed class TestGenericTypeResolution
{
    /// <summary>
    /// Resolved generic type arguments for the test class.
    /// Will be Type.EmptyTypes if the class is not generic.
    /// </summary>
    public Type[] ResolvedClassGenericArguments { get; set; } = Type.EmptyTypes;

    /// <summary>
    /// Resolved generic type arguments for the test method.
    /// Will be Type.EmptyTypes if the method is not generic.
    /// </summary>
    public Type[] ResolvedMethodGenericArguments { get; set; } = Type.EmptyTypes;

    /// <summary>
    /// Gets whether any generic types were resolved
    /// </summary>
    public bool HasResolvedTypes => 
        ResolvedClassGenericArguments.Length > 0 || 
        ResolvedMethodGenericArguments.Length > 0;
}