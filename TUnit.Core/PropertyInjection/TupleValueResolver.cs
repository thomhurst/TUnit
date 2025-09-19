using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TUnit.Core.Helpers;

namespace TUnit.Core.PropertyInjection;

/// <summary>
/// Responsible for resolving tuple values for property injection.
/// Handles tuple creation and type conversion following DRY principle.
/// </summary>
internal static class TupleValueResolver
{
    /// <summary>
    /// Resolves a tuple value from data source arguments for a specific property type.
    /// </summary>
    /// <param name="propertyType">The expected property type</param>
    /// <param name="args">The arguments from the data source</param>
    /// <returns>The resolved value, potentially a tuple</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Tuple types are created dynamically")]
    public static object? ResolveTupleValue(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] 
        Type propertyType, 
        object?[]? args)
    {
        if (args == null)
        {
            return null;
        }

        // Handle non-tuple properties
        if (!TupleFactory.IsTupleType(propertyType))
        {
            return args.FirstOrDefault();
        }

        // Handle tuple properties
        if (args.Length > 1)
        {
            // Multiple arguments - create tuple from them
            return TupleFactory.CreateTuple(propertyType, args);
        }
        
        if (args.Length == 1 && args[0] != null && TupleFactory.IsTupleType(args[0]!.GetType()))
        {
            // Single tuple argument - check if it needs type conversion
            var tupleValue = args[0]!;
            var tupleType = tupleValue.GetType();
            
            if (tupleType != propertyType)
            {
                // Tuple types don't match - unwrap and recreate with correct types
                var elements = DataSourceHelpers.UnwrapTupleAot(tupleValue);
                return TupleFactory.CreateTuple(propertyType, elements);
            }
            
            // Types match - use directly
            return tupleValue;
        }
        
        // Single non-tuple argument for tuple property - shouldn't happen but handle gracefully
        return args.FirstOrDefault();
    }
}