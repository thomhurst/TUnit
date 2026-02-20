using System.ComponentModel;

namespace TUnit.Mock.Setup;

/// <summary>
/// Fluent setup builder for property getters.
/// Returned by mock.Setup.PropertyName_Get().
/// This is simply a constrained alias of <see cref="IMethodSetup{TProperty}"/>
/// since property getter setup only needs <c>.Returns(value)</c>.
/// </summary>
public interface IPropertySetup<TProperty> : IMethodSetup<TProperty>;

/// <summary>
/// Fluent setup builder for property setters.
/// Returned by mock.Setup.PropertyName_Set(value).
/// </summary>
public interface IPropertySetterSetup : IVoidMethodSetup;
