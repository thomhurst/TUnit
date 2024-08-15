using TUnit.Analyzers.Extensions;
using TUnit.Core;

namespace TUnit.Analyzers.Helpers;

internal static class WellKnown
{
    public static class AttributeFullyQualifiedClasses
    {
        public static readonly string TimeoutAttribute = GetTypeName(typeof(TimeoutAttribute));
        public static readonly string Explicit = GetTypeName(typeof(ExplicitAttribute));
        public static readonly string Matrix = GetTypeName(typeof(MatrixAttribute));

        public static readonly string BeforeAttribute = GetTypeName(typeof(BeforeAttribute));
        public static readonly string AfterAttribute = GetTypeName(typeof(AfterAttribute));

        public static readonly string BeforeEveryAttribute = GetTypeName(typeof(BeforeEveryAttribute));
        public static readonly string AfterEveryAttribute = GetTypeName(typeof(AfterEveryAttribute));
        
        public static readonly string Test = GetTypeName(typeof(TestAttribute));
        public static readonly string Arguments = GetTypeName(typeof(ArgumentsAttribute));
        public static readonly string MethodDataSource = GetTypeName(typeof(MethodDataSourceAttribute));
        public static readonly string ClassDataSource = GetTypeName(typeof(ClassDataSourceAttribute<>));
        public static readonly string EnumerableMethodDataSource = GetTypeName(typeof(EnumerableMethodDataSourceAttribute));

        public static readonly string TestContext = GetTypeName(typeof(TestContext));
        public static readonly string ClassHookContext = GetTypeName(typeof(ClassHookContext));
        public static readonly string AssemblyHookContext = GetTypeName(typeof(AssemblyHookContext));

        public static readonly string InheritsTestsAttribute = GetTypeName(typeof(InheritsTestsAttribute));

        public static readonly string NotInParallelAttribute = GetTypeName(typeof(NotInParallelAttribute));
        public static readonly string DependsOnAttribute = GetTypeName(typeof(DependsOnAttribute));
        public static readonly string CancellationToken = GetTypeName(typeof(CancellationToken));

        private static string GetTypeName(Type type) => $"global::{type.GetFullNameWithoutGenericArity()}";
    }
}