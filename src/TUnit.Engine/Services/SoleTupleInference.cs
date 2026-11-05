using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Services;

// Sole data tuples can reconstruct their elements at invocation. Optional/params tails
// and ordinary parameters use TestGenericTypeResolver's strict inference instead.
internal static class SoleTupleInference
{
    private enum InferenceMode
    {
        // Walk the row structurally; later occurrences replace earlier bindings.
        Structural,
        // An interface/base candidate must agree with existing bindings and be assignable.
        Candidate
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Type mapping inference uses Type.GetInterfaces and reflection")]
#endif
    internal static bool TryInfer(Type parameterType, Type argumentType, Dictionary<Type, Type> bindings)
        => TryInfer(parameterType, argumentType, bindings, InferenceMode.Structural);

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Type mapping inference uses Type.GetInterfaces and reflection")]
#endif
    private static bool TryMergeCandidate(Type parameterType, Type candidateType, Dictionary<Type, Type> bindings)
    {
        var candidateBindings = new Dictionary<Type, Type>(bindings);
        if (!TryInfer(parameterType, candidateType, candidateBindings, InferenceMode.Candidate)) return false;
        foreach (var binding in candidateBindings) bindings[binding.Key] = binding.Value;
        return true;
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Type mapping inference uses Type.GetInterfaces and reflection")]
#endif
    private static bool TryInfer(
        Type parameterType,
        Type argumentType,
        Dictionary<Type, Type> typeMapping, InferenceMode mode)
    {
        // A populated Nullable<T> boxes as T, including flattened tuple elements.
        if (Nullable.GetUnderlyingType(parameterType) is { } underlyingParameter
            && argumentType.IsValueType && Nullable.GetUnderlyingType(argumentType) is null)
        {
            return TryInfer(underlyingParameter, argumentType, typeMapping, mode);
        }

        // Direct generic parameter
        if (parameterType.IsGenericParameter)
        {
            // Reconstructed tuple elements preserve the generator's last-occurrence inference.
            if (mode == InferenceMode.Structural)
            {
                typeMapping[parameterType] = argumentType;
                return true;
            }
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
            return TryInfer(
                parameterType.GetElementType()!,
                argumentType.GetElementType()!,
                typeMapping, mode);
        }

        // Generic types (e.g., List<T>, Dictionary<K,V>)
        if (parameterType.IsGenericType && argumentType.IsGenericType)
        {
            var paramGenericDef = parameterType.GetGenericTypeDefinition();
            var argGenericDef = argumentType.IsGenericTypeDefinition
                ? argumentType
                : argumentType.GetGenericTypeDefinition();

            // The tuple binder reconstructs a sole tuple argument across reference/value families.
            // Infer the corresponding elements first, as the source generator does.
            if (paramGenericDef == argGenericDef
                || TupleHelper.IsTupleType(parameterType) && TupleHelper.IsTupleType(argumentType))
            {
                var paramGenericArgs = parameterType.GetGenericArguments();
                var argGenericArgs = argumentType.GetGenericArguments();

                if (paramGenericArgs.Length == argGenericArgs.Length)
                {
                    for (var i = 0; i < paramGenericArgs.Length; i++)
                    {
                        // Candidates validate closed arguments after substitution. Tuple invocation
                        // converts closed elements, so neither path infers bindings from them.
                        if (!paramGenericArgs[i].ContainsGenericParameters
                            && (mode == InferenceMode.Candidate || TupleHelper.IsTupleType(parameterType)))
                            continue;
                        // A sole tuple is reconstructed with conversions. Match the generator's
                        // last-element inference through structurally matching tuple elements.
                        if (TupleHelper.IsTupleType(parameterType))
                        {
                            var parameterElement = paramGenericArgs[i];
                            var argumentElement = argGenericArgs[i];
                            if (Nullable.GetUnderlyingType(parameterElement) is { } nullableElement
                                && argumentElement.IsValueType)
                            {
                                parameterElement = nullableElement;
                                argumentElement = Nullable.GetUnderlyingType(argumentElement) ?? argumentElement;
                            }
                            if (parameterElement.IsGenericParameter)
                            {
                                if (!TryInfer(parameterElement, argumentElement, typeMapping,
                                        mode)) return false;
                                continue;
                            }
                        }
                        if (!TryInfer(paramGenericArgs[i], argGenericArgs[i], typeMapping,
                            mode))
                        {
                            return false;
                        }
                    }
                    if (mode == InferenceMode.Structural) return true;
                    try
                    {
                        return SubstituteCandidateParameters(parameterType, typeMapping).IsAssignableFrom(argumentType);
                    }
                    catch (ArgumentException)
                    {
                        // Inferred arguments can violate this candidate's generic constraints.
                        // Reject it without preventing the caller from trying other candidates.
                        return false;
                    }
                }
            }
        }

        // A reconstructed tuple element may be a derived instance of an open generic base.
        if (parameterType is { IsGenericType: true, IsClass: true })
        {
            var parameterDefinition = parameterType.GetGenericTypeDefinition();
            for (var baseType = argumentType.BaseType; baseType is not null; baseType = baseType.BaseType)
            {
                if (!baseType.IsGenericType || baseType.GetGenericTypeDefinition() != parameterDefinition) continue;
                if (TryMergeCandidate(parameterType, baseType, typeMapping)) return true;
            }
        }

        // Handle case where parameter is a generic interface and argument implements it
        if (parameterType is { IsGenericType: true, IsInterface: true })
        {
            Dictionary<Type, Type>? agreedBindings = null;
            foreach (var candidate in AssemblyReferenceCache.GetInterfaces(argumentType))
            {
                if (!candidate.IsGenericType || candidate.GetGenericTypeDefinition() != parameterType.GetGenericTypeDefinition()) continue;
                var candidateBindings = new Dictionary<Type, Type>(typeMapping);
                if (!TryInfer(parameterType, candidate, candidateBindings, InferenceMode.Candidate)) continue;
                // Interface enumeration order differs between reflection and Roslyn. Only
                // accept candidates that infer the same complete set of bindings.
                if (agreedBindings is not null)
                {
                    if (agreedBindings.Count != candidateBindings.Count) return false;
                    foreach (var binding in candidateBindings)
                        if (!agreedBindings.TryGetValue(binding.Key, out var agreed)
                            || agreed != binding.Value) return false;
                }
                agreedBindings = candidateBindings;
            }
            if (agreedBindings is not null)
            {
                foreach (var binding in agreedBindings) typeMapping[binding.Key] = binding.Value;
                return true;
            }
        }

        // A reconstructed element with no structural match contributes no bindings. Its
        // interface/base declaration may hide a compatible value, so defer the cast to invocation.
        // Interface/base candidate validation never overwrites bindings and remains strict.
        return mode == InferenceMode.Structural || parameterType.IsAssignableFrom(argumentType);
    }

    // Open candidate types are only rebuilt during reflection inference. Validate the entire
    // constructed type: assignability of its individual invariant arguments is insufficient.
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Open generic tuple candidates are resolved only in reflection mode; source-generated tests are already closed.")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "Candidate definitions and inferred arguments come from the test parameter and data types already in use.")]
    private static Type SubstituteCandidateParameters(Type type, Dictionary<Type, Type> bindings)
    {
        if (!type.ContainsGenericParameters) return type;
        if (type.IsGenericParameter) return bindings.TryGetValue(type, out var bound) ? bound : type;
        if (type.IsArray)
        {
            var element = type.GetElementType()!;
            var substituted = SubstituteCandidateParameters(element, bindings);
            return type == element.MakeArrayType()
                ? substituted.MakeArrayType()
                : substituted.MakeArrayType(type.GetArrayRank());
        }
        if (type.IsGenericType)
        {
            var arguments = type.GetGenericArguments();
            for (var i = 0; i < arguments.Length; i++)
                arguments[i] = SubstituteCandidateParameters(arguments[i], bindings);
            return type.GetGenericTypeDefinition().MakeGenericType(arguments);
        }
        return type;
    }
}
