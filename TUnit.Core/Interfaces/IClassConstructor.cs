using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Interfaces;

public interface IClassConstructor
{
    T Create<
#if NET8_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] 
#endif
        T>() where T : class;
    Task DisposeAsync<T>(T t);
}