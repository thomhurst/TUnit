using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Interfaces;

public interface IClassConstructor
{
    T Create<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : class;
    Task DisposeAsync<T>(T t);
}