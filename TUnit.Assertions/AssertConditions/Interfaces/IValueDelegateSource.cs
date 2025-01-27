namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface IValueDelegateSource<out TActual> : IValueSource<TActual>, IDelegateSource<TActual>;