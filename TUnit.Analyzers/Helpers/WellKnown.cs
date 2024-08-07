namespace TUnit.Analyzers.Helpers;

internal static class WellKnown
{
    public static class AttributeFullyQualifiedClasses
    {
        public const string TimeoutAttribute = "global::TUnit.Core.TimeoutAttribute";
        public const string Explicit = "global::TUnit.Core.ExplicitAttribute";
        public const string Matrix = "global::TUnit.Core.MatrixAttribute";

        public const string BeforeAttribute = "global::TUnit.Core.BeforeAttribute";
        public const string AfterAttribute = "global::TUnit.Core.AfterAttribute";
        
        public const string Test = "global::TUnit.Core.TestAttribute";
        public const string Arguments = "global::TUnit.Core.ArgumentsAttribute";
        public const string MethodDataSource = "global::TUnit.Core.MethodDataSourceAttribute";
        public const string ClassDataSource = "global::TUnit.Core.ClassDataSourceAttribute";
        public const string EnumerableMethodDataSource = "global::TUnit.Core.EnumerableMethodDataSourceAttribute";

        public const string TestContext = "global::TUnit.Core.TestContext";
        public const string ClassHookContext = "global::TUnit.Core.Models.ClassHookContext";
        public const string AssemblyHookContext = "global::TUnit.Core.Models.AssemblyHookContext";
        
        public const string InheritsTestsAttribute = "global::TUnit.Core.InheritsTestsAttribute";

        public const string NotInParallelAttribute = "global::TUnit.Core.NotInParallelAttribute";
        public const string DependsOnAttribute = "global::TUnit.Core.DependsOnAttribute";
        public const string CancellationToken = "global::System.Threading.CancellationToken";
    }
}