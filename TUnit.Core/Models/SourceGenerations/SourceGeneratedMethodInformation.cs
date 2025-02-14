using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Helpers;

namespace TUnit.Core;

public record SourceGeneratedMethodInformation : SourceGeneratedMemberInformation
{
    internal static SourceGeneratedMethodInformation Unknown { get; } =
        new()
        {
            Attributes = [],
            Name = "Unknown",
            ReturnType = typeof(void),
            Type = typeof(object),
            Parameters = [],
            GenericTypeCount = 0,
            Class = new SourceGeneratedClassInformation
            {
                Assembly = new SourceGeneratedAssemblyInformation()
                {
                    Attributes = [],
                    Name = "Unknown",
                },
                Attributes = [],
                Name = "Unknown",
                Namespace = "Unknown",
                Parameters = [],
                Properties = [],
                Type = typeof(object)
            }
        };
    
    public required SourceGeneratedParameterInformation[] Parameters { get; init; }
    
    public required int GenericTypeCount { get; init; }
    
    public required SourceGeneratedClassInformation Class { get; init; }

    [field: AllowNull, MaybeNull]
    public MethodInfo ReflectionInformation => field ??= GetMethodInfo();

    private MethodInfo GetMethodInfo()
    {
        return MethodInfoRetriever.GetMethodInfo(Type, Name, GenericTypeCount, Parameters.Select(x => x.Type).ToArray());
    }

    public required Type ReturnType { get; init; }
    
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public override required Type Type { get; init; }
}