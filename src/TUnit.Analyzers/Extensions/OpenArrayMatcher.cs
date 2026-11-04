using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Analyzers.Helpers;
using ArrayBindings = System.Collections.Immutable.ImmutableDictionary<Microsoft.CodeAnalysis.ITypeParameterSymbol, System.Collections.Immutable.ImmutableArray<(Microsoft.CodeAnalysis.ITypeSymbol Type, Microsoft.CodeAnalysis.VarianceKind Variance)>>;

namespace TUnit.Analyzers.Extensions;

// Matches open array elements without rebuilding runtime objects. Tracks identity and
// reference bounds, including tuple siblings and alternative interfaces. Unknown common
// interface/delegate implementations remain accepted; generic constraints are handled separately.
// Conversions involving unsubstituted type parameters use Roslyn's existing constraint
// information; this matcher does not infer or validate new constraints (see #7520).
internal sealed class OpenArrayMatcher
{
    // One bounded search per row: exhaustion accepts conservatively instead of reporting
    // an unproven mismatch. Count recursive visits and bound pairs, including retries.
    private int _remainingSteps = 512;

    internal bool IsExhausted => _remainingSteps < 0;

    private bool TryStep() => --_remainingSteps >= 0;

    // Array elements are not rebuilt as tuples by CastHelper. After inference, constructed
    // element types must agree rather than merely having element-wise numeric conversions.
    // A candidate succeeds only if the rest of the constructed type also matches. This
    // continuation lets interface/base alternatives retry after a later argument conflicts.
    internal bool MatchesArrayElement(Compilation compilation, ITypeSymbol argument, ITypeSymbol parameter,
        ArrayBindings bindings, Func<ArrayBindings, bool> next, bool allowReferenceConversions = true,
        VarianceKind variance = VarianceKind.Out)
    {
        if (!TryStep())
        {
            return true;
        }

        if (parameter is ITypeParameterSymbol typeParameter)
        {
            // Keep variant bounds as well as exact bindings: a later invariant occurrence
            // must satisfy earlier bounds, and vice versa, regardless of traversal order.
            bindings.TryGetValue(typeParameter, out var occurrences);
            if (occurrences.IsDefault)
            {
                occurrences = ImmutableArray<(ITypeSymbol Type, VarianceKind Variance)>.Empty;
            }

            occurrences = occurrences.Add((argument, allowReferenceConversions ? variance : VarianceKind.None));
            foreach (var invariant in occurrences)
            {
                if (invariant.Variance != VarianceKind.None)
                {
                    continue;
                }

                foreach (var occurrence in occurrences)
                {
                    if (!TryStep())
                    {
                        return true;
                    }

                    var source = occurrence.Variance == VarianceKind.In ? invariant.Type : occurrence.Type;
                    var target = occurrence.Variance == VarianceKind.In ? occurrence.Type : invariant.Type;
                    var boundConversion = compilation.ClassifyConversion(source, target);
                    if (!boundConversion.IsIdentity
                        && (occurrence.Variance == VarianceKind.None
                            || !boundConversion.IsImplicit || !boundConversion.IsReference))
                    {
                        return false;
                    }
                }
            }

            // Lower bounds must convert to upper bounds even without an exact binding.
            foreach (var lower in occurrences)
            {
                if (lower.Variance != VarianceKind.Out)
                {
                    continue;
                }

                foreach (var upper in occurrences)
                {
                    if (!TryStep())
                    {
                        return true;
                    }

                    if (upper.Variance == VarianceKind.In)
                    {
                        var boundConversion = compilation.ClassifyConversion(lower.Type, upper.Type);
                        if (!boundConversion.IsIdentity
                            && (!boundConversion.IsImplicit || !boundConversion.IsReference))
                        {
                            return false;
                        }
                    }
                }
            }

            var upperBounds = ImmutableArray.CreateBuilder<ITypeSymbol>();
            foreach (var occurrence in occurrences)
            {
                if (occurrence.Variance == VarianceKind.In
                    && !CompilationExtensions.ContainsTypeParameter(occurrence.Type))
                {
                    upperBounds.Add(occurrence.Type);
                }
            }

            if (!CanShareReferenceSubtype(compilation, upperBounds.ToImmutable()))
            {
                return false;
            }

            return next(bindings.SetItem(typeParameter, occurrences));
        }

        var conversion = compilation.ClassifyConversion(argument, parameter);
        if (conversion.IsIdentity
            || (allowReferenceConversions && conversion.IsImplicit && conversion.IsReference))
        {
            return next(bindings);
        }

        if (argument is IArrayTypeSymbol argumentArray && parameter is IArrayTypeSymbol parameterArray)
        {
            return argumentArray.Rank == parameterArray.Rank
                && argumentArray.IsSZArray == parameterArray.IsSZArray
                && MatchesArrayElement(compilation, argumentArray.ElementType, parameterArray.ElementType,
                    bindings, next, allowReferenceConversions && argumentArray.ElementType.IsReferenceType);
        }

        if (argument is INamedTypeSymbol argumentNamed && parameter is INamedTypeSymbol parameterNamed
            && SymbolEqualityComparer.Default.Equals(argumentNamed.OriginalDefinition, parameterNamed.OriginalDefinition))
        {
            bool MatchArguments(int index, ArrayBindings candidateBindings)
            {
                if (index == parameterNamed.TypeArguments.Length)
                {
                    return next(candidateBindings);
                }

                var argumentType = argumentNamed.TypeArguments[index];
                var parameterType = parameterNamed.TypeArguments[index];
                Func<ArrayBindings, bool> remaining = candidate => MatchArguments(index + 1, candidate);
                // Infer an open target before reversing contravariant conversion direction.
                if (parameterType is ITypeParameterSymbol)
                {
                    return MatchesArrayElement(compilation, argumentType, parameterType, candidateBindings, remaining,
                        allowReferenceConversions: allowReferenceConversions && argumentType.IsReferenceType
                            && parameterNamed.TypeParameters[index].Variance != VarianceKind.None,
                        variance: parameterNamed.TypeParameters[index].Variance);
                }

                // Variance permits reference conversions only; value types still require identity.
                var allowVariance = allowReferenceConversions && argumentType.IsReferenceType
                    && parameterType.IsReferenceType;
                var argumentVariance = allowVariance ? parameterNamed.TypeParameters[index].Variance : VarianceKind.None;
                return MatchesArrayElement(compilation,
                    argumentVariance == VarianceKind.In ? parameterType : argumentType,
                    argumentVariance == VarianceKind.In ? argumentType : parameterType,
                    candidateBindings, remaining, allowReferenceConversions: argumentVariance != VarianceKind.None);
            }

            // A nested type's own TypeArguments omit its containing type's construction.
            if (parameterNamed.ContainingType is { } containingType)
            {
                return argumentNamed.ContainingType is { } argumentContainingType
                    && MatchesArrayElement(compilation, argumentContainingType, containingType,
                        bindings, candidate => MatchArguments(0, candidate), allowReferenceConversions: false);
            }

            return MatchArguments(0, bindings);
        }

        // Reference array covariance can expose the inferred type through an implemented
        // interface or base class, without rebuilding any array elements.
        if (allowReferenceConversions && argument.IsReferenceType && parameter.IsReferenceType)
        {
            foreach (var interfaceType in argument.AllInterfaces)
            {
                if (MatchesArrayElement(compilation, interfaceType, parameter, bindings, next))
                {
                    return true;
                }
            }

            if (argument.BaseType is { } baseType)
            {
                return MatchesArrayElement(compilation, baseType, parameter, bindings, next);
            }
        }

        return false;
    }

    private bool CanShareReferenceSubtype(Compilation compilation, ImmutableArray<ITypeSymbol> bounds)
    {
        if (!TryStep())
        {
            return true;
        }

        IArrayTypeSymbol? array = null;
        for (var i = 0; i < bounds.Length; i++)
        {
            array ??= bounds[i] as IArrayTypeSymbol;
            for (var j = i + 1; j < bounds.Length; j++)
            {
                if (!TryStep())
                {
                    return true;
                }

                if (!CanShareReferenceSubtype(compilation, bounds[i], bounds[j]))
                {
                    return false;
                }
            }
        }

        if (IsExhausted)
        {
            return true;
        }

        // A common delegate subtype uses one construction. Project every covariant
        // argument together, so nested arrays/interfaces cannot pick different witnesses
        // for each pair. Pairwise validation above checks definition, containers and identity.
        INamedTypeSymbol? delegateBound = null;
        foreach (var bound in bounds)
        {
            if (bound is INamedTypeSymbol { TypeKind: TypeKind.Delegate } candidate)
            {
                delegateBound = candidate;
                break;
            }
        }

        if (delegateBound is not null)
        {
            for (var i = 0; i < delegateBound.TypeArguments.Length; i++)
            {
                if (delegateBound.TypeParameters[i].Variance != VarianceKind.Out
                    || !delegateBound.TypeArguments[i].IsReferenceType)
                {
                    continue;
                }

                var argumentBounds = ImmutableArray.CreateBuilder<ITypeSymbol>();
                foreach (var bound in bounds)
                {
                    if (bound is INamedTypeSymbol { TypeKind: TypeKind.Delegate } other)
                    {
                        argumentBounds.Add(other.TypeArguments[i]);
                    }
                }

                if (!CanShareReferenceSubtype(compilation, argumentBounds.ToImmutable()))
                {
                    return false;
                }
            }
        }

        if (array is null || !array.ElementType.IsReferenceType)
        {
            return true;
        }

        // An array common subtype has one element type satisfying every array and
        // generic array-interface bound together. Pairwise checks alone can choose
        // incompatible elements for IList<string> and IList<Uri> against object[].
        var elementBounds = ImmutableArray.CreateBuilder<ITypeSymbol>();
        foreach (var bound in bounds)
        {
            if (bound is IArrayTypeSymbol arrayBound)
            {
                elementBounds.Add(arrayBound.ElementType);
            }
            else if (bound is INamedTypeSymbol { TypeKind: TypeKind.Interface, TypeArguments.Length: 1 } target)
            {
                foreach (var implemented in array.AllInterfaces)
                {
                    if (SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, target.OriginalDefinition))
                    {
                        elementBounds.Add(target.TypeArguments[0]);
                        break;
                    }
                }
            }
        }

        return CanShareReferenceSubtype(compilation, elementBounds.ToImmutable());
    }

    // Conservative upper-bound check: unknown interface/delegate implementations remain possible.
    private bool CanShareReferenceSubtype(Compilation compilation, ITypeSymbol left, ITypeSymbol right)
    {
        if (!TryStep())
        {
            return true;
        }

        var forward = compilation.ClassifyConversion(left, right);
        var reverse = compilation.ClassifyConversion(right, left);
        if (forward.IsIdentity || (forward.IsImplicit && forward.IsReference)
            || (reverse.IsImplicit && reverse.IsReference))
        {
            return true;
        }

        if (left is IArrayTypeSymbol leftArray && right is IArrayTypeSymbol rightArray)
        {
            return leftArray.Rank == rightArray.Rank && leftArray.IsSZArray == rightArray.IsSZArray
                && leftArray.ElementType.IsReferenceType && rightArray.ElementType.IsReferenceType
                && CanShareReferenceSubtype(compilation, leftArray.ElementType, rightArray.ElementType);
        }

        // Arrays cannot derive from an arbitrary class. Their actual base classes
        // (object and Array) were already accepted by the conversions above.
        if ((left is IArrayTypeSymbol && right.TypeKind == TypeKind.Class)
            || (right is IArrayTypeSymbol && left.TypeKind == TypeKind.Class))
        {
            return false;
        }

        if (left is IArrayTypeSymbol array && right is INamedTypeSymbol { TypeKind: TypeKind.Interface } arrayInterface)
        {
            return CanArraySubtypeImplement(compilation, array, arrayInterface);
        }

        if (right is IArrayTypeSymbol otherArray && left is INamedTypeSymbol { TypeKind: TypeKind.Interface } otherInterface)
        {
            return CanArraySubtypeImplement(compilation, otherArray, otherInterface);
        }

        // Delegate variance can share a constructed subtype only within the same
        // delegate definition. Delegates cannot gain other bases or interfaces;
        // conversions to Delegate, MulticastDelegate and ICloneable passed above.
        if (left.TypeKind == TypeKind.Delegate || right.TypeKind == TypeKind.Delegate)
        {
            if (left is not INamedTypeSymbol { TypeKind: TypeKind.Delegate } leftDelegate
                || right is not INamedTypeSymbol { TypeKind: TypeKind.Delegate } rightDelegate
                || !SymbolEqualityComparer.Default.Equals(leftDelegate.OriginalDefinition, rightDelegate.OriginalDefinition))
            {
                return false;
            }

            // Containing constructions are invariant even when the delegate's own
            // arguments support variance (Outer<string>.D and Outer<object>.D differ).
            // Roslyn identity also preserves equivalent dynamic/object constructions.
            if (!SymbolEqualityComparer.Default.Equals(leftDelegate.ContainingType, rightDelegate.ContainingType)
                && (leftDelegate.ContainingType is not { } leftContaining
                    || rightDelegate.ContainingType is not { } rightContaining
                    || !compilation.ClassifyConversion(leftContaining, rightContaining).IsIdentity))
            {
                return false;
            }

            for (var i = 0; i < leftDelegate.TypeArguments.Length; i++)
            {
                var leftArgument = leftDelegate.TypeArguments[i];
                var rightArgument = rightDelegate.TypeArguments[i];
                if (SymbolEqualityComparer.Default.Equals(leftArgument, rightArgument))
                {
                    continue;
                }

                // Value arguments and invariant positions require identity. Covariant
                // positions need a shared subtype; contravariant positions can use object
                // as a common reference supertype (e.g. Action<string>/Action<Uri>).
                var variance = leftDelegate.TypeParameters[i].Variance;
                if (!leftArgument.IsReferenceType || !rightArgument.IsReferenceType
                    || variance == VarianceKind.None
                    || (variance == VarianceKind.Out
                        && !CanShareReferenceSubtype(compilation, leftArgument, rightArgument)))
                {
                    return false;
                }
            }

            return true;
        }

        var leftSealedClass = left.IsSealed && left.TypeKind is not (TypeKind.Array or TypeKind.Delegate);
        var rightSealedClass = right.IsSealed && right.TypeKind is not (TypeKind.Array or TypeKind.Delegate);
        return !leftSealedClass && !rightSealedClass
            && !(left.TypeKind == TypeKind.Class && right.TypeKind == TypeKind.Class);
    }

    private bool CanArraySubtypeImplement(Compilation compilation, IArrayTypeSymbol array, INamedTypeSymbol target)
    {
        // An array subtype can change its reference element type, but cannot acquire
        // arbitrary interfaces. Non-generic interfaces were handled by conversion above.
        foreach (var implemented in array.AllInterfaces)
        {
            if (implemented.TypeArguments.Length == 1 && target.TypeArguments.Length == 1
                && SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, target.OriginalDefinition)
                && array.ElementType.IsReferenceType && target.TypeArguments[0].IsReferenceType
                && CanShareReferenceSubtype(compilation, array.ElementType, target.TypeArguments[0]))
            {
                return true;
            }
        }

        return false;
    }
}
