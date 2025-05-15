using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Interfaces;

public interface IClassConstructor
{
    object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ClassConstructorMetadata classConstructorMetadata);
}