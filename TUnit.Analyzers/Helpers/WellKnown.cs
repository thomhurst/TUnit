namespace TUnit.Analyzers.Helpers;

public static class WellKnown
{
    public static class AttributeFullyQualifiedClasses
    {
        public static readonly FullyQualifiedTypeName TimeoutAttribute = GetTypeName("TimeoutAttribute");
        public static readonly FullyQualifiedTypeName Explicit = GetTypeName("ExplicitAttribute");
        public static readonly FullyQualifiedTypeName Matrix = GetTypeName("MatrixAttribute");

        public static readonly FullyQualifiedTypeName BeforeAttribute = GetTypeName("BeforeAttribute");
        public static readonly FullyQualifiedTypeName AfterAttribute = GetTypeName("AfterAttribute");

        public static readonly FullyQualifiedTypeName BeforeEveryAttribute = GetTypeName("BeforeEveryAttribute");
        public static readonly FullyQualifiedTypeName AfterEveryAttribute = GetTypeName("AfterEveryAttribute");
        
        public static readonly FullyQualifiedTypeName Test = GetTypeName("TestAttribute");
        public static readonly FullyQualifiedTypeName Arguments = GetTypeName("ArgumentsAttribute");
        public static readonly FullyQualifiedTypeName MethodDataSource = GetTypeName("MethodDataSourceAttribute");
        public static readonly FullyQualifiedTypeName ClassDataSource = GetTypeName("ClassDataSourceAttribute");
        public static readonly FullyQualifiedTypeName ClassConstructor = GetTypeName("ClassConstructorAttribute");

        public static readonly FullyQualifiedTypeName TestContext = GetTypeName("TestContext");
        public static readonly FullyQualifiedTypeName ClassHookContext = GetTypeName("ClassHookContext");
        public static readonly FullyQualifiedTypeName AssemblyHookContext = GetTypeName("AssemblyHookContext");

        public static readonly FullyQualifiedTypeName InheritsTestsAttribute = GetTypeName("InheritsTestsAttribute");

        public static readonly FullyQualifiedTypeName NotInParallelAttribute = GetTypeName("NotInParallelAttribute");
        public static readonly FullyQualifiedTypeName DependsOnAttribute = GetTypeName("DependsOnAttribute");
        
        public static readonly FullyQualifiedTypeName IDataAttribute = GetTypeName("IDataAttribute");
        public static readonly FullyQualifiedTypeName IDataSourceGeneratorAttribute = GetTypeName("IDataSourceGeneratorAttribute");

        public static readonly FullyQualifiedTypeName CancellationToken = new("System.Threading.CancellationToken");

        private static FullyQualifiedTypeName GetTypeName(string className) => new($"TUnit.Core.{className}");
    }
}