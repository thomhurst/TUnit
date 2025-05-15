using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Extensions;

public static class ClassConstructorExtensions
{
    public static T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IClassConstructor classConstructor,
        ClassConstructorMetadata classConstructorMetadata) where T : class
    {
        return (T)classConstructor.Create(typeof(T), classConstructorMetadata);
    }
}