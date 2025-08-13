using TUnit.Core.Enums;

namespace TUnit.Core.Helpers;

/// <summary>
/// Helper class for resolving test class types from data generator metadata
/// </summary>
public static class TestClassTypeHelper
{
    /// <summary>
    /// Gets the test class type from DataGeneratorMetadata using the new pattern
    /// </summary>
    /// <param name="dataGeneratorMetadata">The data generator metadata</param>
    /// <returns>The test class type, or null if it cannot be determined</returns>
    public static Type GetTestClassType(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Try to get from TestInformation first (primary)
        if (dataGeneratorMetadata.TestInformation?.Class.Type != null)
        {
            return dataGeneratorMetadata.TestInformation.Class.Type;
        }

        // For property injection, use the containing type from the property metadata (fallback)
        if (dataGeneratorMetadata.Type == DataGeneratorType.Property && dataGeneratorMetadata.MembersToGenerate.Length > 0)
        {
            if (dataGeneratorMetadata.MembersToGenerate[0] is PropertyMetadata propertyMetadata)
            {
                return propertyMetadata.ContainingTypeMetadata.Type;
            }
        }

        return typeof(object);
    }
}
