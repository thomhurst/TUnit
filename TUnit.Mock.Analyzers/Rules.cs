using Microsoft.CodeAnalysis;

namespace TUnit.Mock.Analyzers;

public static class Rules
{
    public static readonly DiagnosticDescriptor TM001_CannotMockSealedType = new(
        id: "TM001",
        title: "Cannot mock sealed type",
        messageFormat: "Cannot mock sealed type '{0}'. Consider extracting an interface.",
        category: "TUnit.Mock",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor TM002_CannotMockValueType = new(
        id: "TM002",
        title: "Cannot mock value type",
        messageFormat: "Cannot mock value type '{0}'. Mocking requires reference types.",
        category: "TUnit.Mock",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
