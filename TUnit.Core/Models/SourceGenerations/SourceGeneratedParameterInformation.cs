namespace TUnit.Core;

public record SourceGeneratedParameterInformation<T>() : SourceGeneratedParameterInformation(typeof(T));

public record SourceGeneratedParameterInformation(Type Type) : SourceGeneratedMemberInformation;
    