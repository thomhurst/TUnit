namespace TUnit.Analyzers;

/// <summary>
/// Diagnostic ID constants for all TUnit analyzer rules.
/// </summary>
/// <remarks>
/// Code fix providers (TUnit.Analyzers.CodeFixers) MUST reference these constants instead of
/// <c>Rules.X.Id</c>. Constants are baked into the consuming assembly at compile time, so the
/// code fixers carry no runtime reference to the <see cref="Rules"/> type. A runtime reference
/// can bind against a stale TUnit.Analyzers.dll already loaded in Visual Studio (analyzers
/// cannot be unloaded), throwing <see cref="System.MissingFieldException"/> for newly added
/// rules. See https://github.com/thomhurst/TUnit/issues/6157.
/// </remarks>
public static class DiagnosticIds
{
    public const string WrongArgumentTypeTestData = "TUnit0001";
    public const string NoTestDataProvided = "TUnit0002";
    public const string NoMethodFound = "TUnit0004";
    public const string MethodParameterBadNullability = "TUnit0005";
    public const string MethodMustBeStatic = "TUnit0007";
    public const string MethodMustBePublic = "TUnit0008";
    public const string MethodMustNotBeAbstract = "TUnit0009";
    public const string MethodMustBeParameterless = "TUnit0010";
    public const string MethodMustReturnData = "TUnit0011";
    public const string TooManyArgumentsInTestMethod = "TUnit0013";
    public const string PublicMethodMissingTestAttribute = "TUnit0014";
    public const string MissingTimeoutCancellationTokenAttributes = "TUnit0015";
    public const string MethodMustNotBeStatic = "TUnit0016";
    public const string ConflictingExplicitAttributes = "TUnit0017";
    public const string InstanceAssignmentInTestClass = "TUnit0018";
    public const string MissingTestAttribute = "TUnit0019";
    public const string Dispose_Member_In_Cleanup = "TUnit0023";
    public const string UnknownParameters = "TUnit0027";
    public const string DoNotOverrideAttributeUsageMetadata = "TUnit0028";
    public const string DuplicateSingleAttribute = "TUnit0029";
    public const string DoesNotInheritTestsWarning = "TUnit0030";
    public const string AsyncVoidMethod = "TUnit0031";
    public const string DependsOnConflicts = "TUnit0033";
    public const string NoMainMethod = "TUnit0034";
    public const string NoDataSourceProvided = "TUnit0038";
    public const string SingleTestContextParameterRequired = "TUnit0039";
    public const string SingleClassHookContextParameterRequired = "TUnit0040";
    public const string SingleAssemblyHookContextParameterRequired = "TUnit0041";
    public const string GlobalHooksSeparateClass = "TUnit0042";
    public const string PropertyRequiredNotSet = "TUnit0043";
    public const string MustHavePropertySetter = "TUnit0044";
    public const string TooManyDataAttributes = "TUnit0045";
    public const string ReturnFunc = "TUnit0046";
    public const string AsyncLocalCallFlowValues = "TUnit0047";
    public const string InstanceTestMethod = "TUnit0048";
    public const string MatrixDataSourceAttributeRequired = "TUnit0049";
    public const string TooManyArguments = "TUnit0050";
    public const string TypeMustBePublic = "TUnit0051";
    public const string MultipleConstructorsWithoutTestConstructor = "TUnit0052";
    public const string XunitMigration = "TUXU0001";
    public const string NUnitMigration = "TUNU0001";
    public const string MSTestMigration = "TUMS0001";
    public const string OverwriteConsole = "TUnit0055";
    public const string InstanceMethodSource = "TUnit0056";
    public const string HookContextParameterOptional = "TUnit0057";
    public const string HookUnknownParameters = "TUnit0058";
    public const string AbstractTestClassWithDataSources = "TUnit0059";
    public const string PotentialEmptyDataSource = "TUnit0060";
    public const string NoAccessibleConstructor = "TUnit0061";
    public const string CancellationTokenMustBeLastParameter = "TUnit0062";
    public const string CombinedDataSourceAttributeRequired = "TUnit0070";
    public const string CombinedDataSourceMissingParameterDataSource = "TUnit0071";
    public const string CombinedDataSourceConflictWithMatrix = "TUnit0072";
    public const string MissingPolyfillPackage = "TUnit0073";
    public const string RedundantHookAttributeOnOverride = "TUnit0074";
}
