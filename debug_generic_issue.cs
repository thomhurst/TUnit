using System;
using System.Linq;

namespace DebugGenericIssue
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Debugging Generic Type Issue");
            Console.WriteLine("============================");
            
            // The issue is that for generic classes like SimpleGenericClassTests<T>,
            // the source generator skips them because:
            // 1. TestMetadataGenerator.GetInheritsTestsClassMetadata returns null for generic types
            // 2. This happens at line: if (classSymbol is { IsGenericType: true, TypeParameters.Length: > 0 })
            
            // When the generic type is skipped, no test metadata is generated
            // Therefore, no DataCombinationGenerator is created
            // And thus, methodDataSources will be 0 because the method isn't even processed
            
            Console.WriteLine("\nThe flow is:");
            Console.WriteLine("1. TestMetadataGenerator processes classes with [Test] methods");
            Console.WriteLine("2. For SimpleGenericClassTests<T>, it sees IsGenericType=true");
            Console.WriteLine("3. It returns null from GetInheritsTestsClassMetadata");
            Console.WriteLine("4. No test metadata is generated for this class");
            Console.WriteLine("5. No DataCombinationGenerator is emitted");
            Console.WriteLine("6. At runtime, there's no test to discover");
            
            Console.WriteLine("\nSolution:");
            Console.WriteLine("For generic test classes, we need to:");
            Console.WriteLine("1. Allow the TestMetadataGenerator to process generic types");
            Console.WriteLine("2. Emit metadata that includes generic type information");
            Console.WriteLine("3. Let the DataCombinationGenerator infer types from Arguments attributes");
            Console.WriteLine("4. Create concrete test instances at runtime based on inferred types");
        }
    }
}