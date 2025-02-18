using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public record SourceGeneratedClassInformation : SourceGeneratedMemberInformation
{
    private static readonly ConcurrentDictionary<string, SourceGeneratedClassInformation> Cache = [];
    public static SourceGeneratedClassInformation GetOrAdd(string name, Func<SourceGeneratedClassInformation> factory)
    {
        return Cache.GetOrAdd(name, _ => factory());
    }
    
    public virtual bool Equals(SourceGeneratedClassInformation? other)
    {
        return Namespace == other?.Namespace 
               && Assembly.Equals(other?.Assembly)
               && base.Equals(other);
    }
    
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Namespace?.GetHashCode() ?? 1;
            hashCode = (hashCode * 397) ^ Assembly.GetHashCode();
            return hashCode;
        }
    }

    public override required Type Type { get; init; }

    public required string? Namespace {get; init;}
    public required SourceGeneratedAssemblyInformation Assembly { get; init; }
    public required SourceGeneratedParameterInformation[] Parameters { get; init; }
    
    public required SourceGeneratedPropertyInformation[] Properties { get; init; }
}