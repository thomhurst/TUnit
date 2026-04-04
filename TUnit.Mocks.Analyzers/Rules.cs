using Microsoft.CodeAnalysis;

namespace TUnit.Mocks.Analyzers;

public static class Rules
{
    public static readonly DiagnosticDescriptor TM001_CannotMockSealedType = new(
        id: "TM001",
        title: "Cannot mock sealed type",
        messageFormat: "Cannot mock sealed type '{0}'. Consider extracting an interface.",
        category: "TUnit.Mocks",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "TUnit.Mocks generates a subclass to intercept calls. Sealed types cannot be subclassed, so they cannot be mocked directly. Extract an interface or abstract class instead."
    );

    public static readonly DiagnosticDescriptor TM002_CannotMockValueType = new(
        id: "TM002",
        title: "Cannot mock value type",
        messageFormat: "Cannot mock value type '{0}'. Mocking requires reference types.",
        category: "TUnit.Mocks",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "TUnit.Mocks generates a subclass to intercept calls. Value types (structs, enums) cannot be subclassed. Use an interface or class instead."
    );

    public static readonly DiagnosticDescriptor TM003_OfDelegateRequiresDelegateType = new(
        id: "TM003",
        title: "Mock.OfDelegate<T>() requires a delegate type",
        messageFormat: "Mock.OfDelegate<T>() requires T to be a delegate type, but '{0}' is not a delegate.",
        category: "TUnit.Mocks",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Mock.OfDelegate<T>() is designed specifically for mocking delegate types. Use Mock.Of<T>() for interfaces and classes."
    );

    public static readonly DiagnosticDescriptor TM004_RequiresCSharp14 = new(
        id: "TM004",
        title: "TUnit.Mocks requires C# 14 or later",
        messageFormat: "TUnit.Mocks requires C# 14 or later (LangVersion 14 or preview). Current language version is '{0}'.",
        category: "TUnit.Mocks",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "TUnit.Mocks uses C# 14 extension members in its generated code. Set <LangVersion>14</LangVersion> or <LangVersion>preview</LangVersion> in your project file.",
        customTags: new[] { WellKnownDiagnosticTags.CompilationEnd }
    );

    public static readonly DiagnosticDescriptor TM005_ArgIsNullNonNullableValueType = new(
        id: "TM005",
        title: "Arg.IsNull/IsNotNull used with non-nullable value type",
        messageFormat: "Arg.{0}<{1}>() will never match because '{1}' is a non-nullable value type. Use '{1}?' instead.",
        category: "TUnit.Mocks",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Non-nullable value types can never be null, so Arg.IsNull<T>() will always return false and Arg.IsNotNull<T>() will always return true. Use the nullable form (e.g. int?) to match nullable value type parameters."
    );
}
