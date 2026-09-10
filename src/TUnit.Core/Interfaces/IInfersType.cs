namespace TUnit.Core.Interfaces;

/// <summary>
/// Marker interface that indicates an attribute provides type information
/// that can be used for generic type inference during source generation.
/// </summary>
/// <typeparam name="T">The type that this attribute infers for generic parameters</typeparam>
public interface IInfersType<T>
{
}