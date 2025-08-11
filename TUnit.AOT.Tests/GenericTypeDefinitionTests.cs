using System;
using System.Collections.Generic;

namespace TUnit.AOT.Tests
{
    /// <summary>
    /// Tests to verify that non-generic types like System.Type are not treated as generic type definitions
    /// </summary>
    public class GenericTypeDefinitionTests
    {
        [Test]
        public void TestMakeGenericType_WithNonGenericType_ShouldCompile()
        {
            // This test verifies that the AotTypeResolver doesn't generate invalid code
            // when MakeGenericType is used with System.Type as an argument (not as the generic definition)
            
            Type listGenericDefinition = typeof(List<>);
            Type typeOfType = typeof(Type);
            
            // This call should not cause the generator to treat System.Type as a generic type definition
            Type listOfType = listGenericDefinition.MakeGenericType(typeOfType);
            
            Console.WriteLine($"Successfully created: {listOfType}");
            
            // Verify the constructed type is correct
            if (listOfType != typeof(List<Type>))
            {
                throw new InvalidOperationException("Type construction failed");
            }
        }

        [Test]
        public void TestNonGenericType_ShouldNotBeUsedAsGenericDefinition()
        {
            // Verify that System.Type itself is not a generic type
            Type typeOfType = typeof(Type);
            
            if (typeOfType.IsGenericType)
            {
                throw new InvalidOperationException("System.Type should not be considered a generic type");
            }
            
            if (typeOfType.IsGenericTypeDefinition)
            {
                throw new InvalidOperationException("System.Type should not be considered a generic type definition");
            }
            
            Console.WriteLine("System.Type correctly identified as non-generic");
        }
    }
}