using TUnit.Core.SourceGenerator.Enums;

namespace TUnit.Core.SourceGenerator.Models;

public record HooksDataModel
{
    public required string FullyQualifiedTypeName { get; init; }
    public required HookLocationType HookLocationType { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string MethodName { get; init; }
    public required string[] ParameterTypes { get; init; }
    public required string HookLevel { get; init; }
    public required string? HookExecutor { get; init; }
    
    public required string FilePath { get; init; }
    
    public required int LineNumber { get; init; }
    
    public required int Order { get; init; }
    public required bool IsEveryHook { get; init; }

    public virtual bool Equals(HooksDataModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return FullyQualifiedTypeName == other.FullyQualifiedTypeName && MinimalTypeName == other.MinimalTypeName && MethodName == other.MethodName && ParameterTypes.SequenceEqual(other.ParameterTypes) && HookLevel == other.HookLevel;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = FullyQualifiedTypeName.GetHashCode();
            hashCode = (hashCode * 397) ^ MinimalTypeName.GetHashCode();
            hashCode = (hashCode * 397) ^ MethodName.GetHashCode();
            hashCode = (hashCode * 397) ^ ParameterTypes.GetHashCode();
            hashCode = (hashCode * 397) ^ HookLevel.GetHashCode();
            return hashCode;
        }
    }
}