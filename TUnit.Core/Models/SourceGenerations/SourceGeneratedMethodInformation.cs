using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Helpers;

namespace TUnit.Core;

public record SourceGeneratedMethodInformation : SourceGeneratedMemberInformation
{
    internal static SourceGeneratedMethodInformation Failure<TClassType>(string methodName) =>
        new()
        {
            Attributes = [],
            Name = methodName,
            ReturnType = typeof(void),
            Type = typeof(object),
            Parameters = [],
            GenericTypeCount = 0,
            Class = new SourceGeneratedClassInformation
            {
                Assembly = new SourceGeneratedAssemblyInformation
                {
                    Attributes = [],
                    Name = typeof(TClassType).Assembly.GetName().Name!,
                },
                Attributes = [],
                Name = typeof(TClassType).Name,
                Namespace = typeof(TClassType).Namespace,
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