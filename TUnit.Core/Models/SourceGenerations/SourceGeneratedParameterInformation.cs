using System.Diagnostics;

namespace TUnit.Core;

[DebuggerDisplay("{Type} {Name})")]
public record SourceGeneratedParameterInformation<T>() : SourceGeneratedParameterInformation(typeof(T));

[DebuggerDisplay("{Type} {Name})")]
public record SourceGeneratedParameterInformation(Type Type) : SourceGeneratedMemberInformation;
    