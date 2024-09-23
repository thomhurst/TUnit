namespace TUnit.Analyzers.Helpers;

internal static class WellKnown
{
    public static class AttributeFullyQualifiedClasses
    {
        public static readonly string TimeoutAttribute = GetTypeName("TimeoutAttribute");
        public static readonly string Explicit = GetTypeName("ExplicitAttribute");
        public static readonly string Matrix = GetTypeName("MatrixAttribute");

        public static readonly string BeforeAttribute = GetTypeName("BeforeAttribute");
        public static readonly string AfterAttribute = GetTypeName("AfterAttribute");

        public static readonly string BeforeEveryAttribute = GetTypeName("BeforeEveryAttribute");
        public static readonly string AfterEveryAttribute = GetTypeName("AfterEveryAttribute");
        
        public static readonly string Test = GetTypeName("TestAttribute");
        public static readonly string Arguments = GetTypeName("ArgumentsAttribute");
        public static readonly string MethodDataSource = GetTypeName("MethodDataSourceAttribute");
        public static readonly string ClassDataSource = GetTypeName("ClassDataSourceAttribute");
        public static readonly string ClassConstructor = GetTypeName("ClassConstructorAttribute");

        public static readonly string TestContext = GetTypeName("TestContext");
        public static readonly string ClassHookContext = GetTypeName("ClassHookContext");
        public static readonly string AssemblyHookContext = GetTypeName("AssemblyHookContext");

        public static readonly string InheritsTestsAttribute = GetTypeName("InheritsTestsAttribute");

        public static readonly string NotInParallelAttribute = GetTypeName("NotInParallelAttribute");
        public static readonly string DependsOnAttribute = GetTypeName("DependsOnAttribute");
        public static readonly string CancellationToken = "global::System.Threading.CancellationToken";

        private static string GetTypeName(string className) => $"global::TUnit.Core.{className}";
    }
}