using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Interfaces;

public interface IClassConstructor
{
    T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(ClassConstructorMetadata classConstructorMetadata) where T : class;
}