﻿namespace TUnit.Analyzers.Helpers;

internal static class WellKnown
{
    public static class AttributeFullyQualifiedClasses
    {
        public const string TimeoutAttribute = "global::TUnit.Core.TimeoutAttribute";
        public const string Explicit = "global::TUnit.Core.ExplicitAttribute";
        public const string CombinativeValues = "global::TUnit.Core.CombinativeValuesAttribute";

        public const string OnceOnlySetUp = "global::TUnit.Core.OnlyOnceSetUpAttribute";
        public const string OnceOnlyCleanUp = "global::TUnit.Core.OnlyOnceCleanUpAttribute";
        public const string AssemblySetUp = "global::TUnit.Core.AssemblySetUpAttribute";
        public const string AssemblyCleanUp = "global::TUnit.Core.AssemblyCleanUpAttribute";
        
        public const string CombinativeTest = "global::TUnit.Core.CombinativeTestAttribute";
        public const string Test = "global::TUnit.Core.TestAttribute";
        public const string DataDrivenTest = "global::TUnit.Core.DataDrivenTestAttribute";
        public const string DataSourceDrivenTest = "global::TUnit.Core.DataSourceDrivenTestAttribute";
        
        public static readonly string[] TestAttributes =
        [
            Test, DataDrivenTest, DataSourceDrivenTest, CombinativeTest
        ];
    }
}