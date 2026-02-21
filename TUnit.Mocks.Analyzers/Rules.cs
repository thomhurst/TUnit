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
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor TM002_CannotMockValueType = new(
        id: "TM002",
        title: "Cannot mock value type",
        messageFormat: "Cannot mock value type '{0}'. Mocking requires reference types.",
        category: "TUnit.Mocks",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor TM003_OfDelegateRequiresDelegateType = new(
        id: "TM003",
        title: "Mock.OfDelegate<T>() requires a delegate type",
        messageFormat: "Mock.OfDelegate<T>() requires T to be a delegate type, but '{0}' is not a delegate.",
        category: "TUnit.Mocks",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
