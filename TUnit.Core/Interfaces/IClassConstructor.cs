namespace TUnit.Core.Interfaces;

public interface IClassConstructor
{
    T Create<T>() where T : class;
    Task DisposeAsync<T>(T t);
}