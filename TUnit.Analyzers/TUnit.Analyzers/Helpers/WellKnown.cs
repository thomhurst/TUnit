namespace TUnit.Analyzers.Helpers;

internal static class WellKnown
{
    public static class AttributeFullyQualifiedClasses
    {
        public static readonly string Explicit = "global::TUnit.Core.ExplicitAttribute";
        public static readonly string CombinativeValues = "global::TUnit.Core.CombinativeValuesAttribute";
        
        public static readonly string CombinativeTest = "global::TUnit.Core.CombinativeTestAttribute";
        public static readonly string Test = "global::TUnit.Core.TestAttribute";
        public static readonly string DataDrivenTest = "global::TUnit.Core.DataDrivenTestAttribute";
        public static readonly string DataSourceDrivenTest = "global::TUnit.Core.DataSourceDrivenTestAttribute";

        public static readonly string[] TestAttributes =
        [
            Test, DataDrivenTest, DataSourceDrivenTest, CombinativeTest
        ];
    }
}