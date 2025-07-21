using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Extensions;

public static class ClassConstructorExtensions
{
    public static async Task<T> Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IClassConstructor classConstructor,
        ClassConstructorMetadata classConstructorMetadata) where T : class
    {
        var instance = await classConstructor.Create(typeof(T), classConstructorMetadata);
        return (T) instance;
    }
}
