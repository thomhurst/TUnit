using System.ComponentModel;

namespace TUnit.Mocks.Setup;

/// <summary>
/// Fluent setup builder for property getters.
/// Returned by <c>mock.Setup.PropertyName.Getter</c>.
/// This is simply a constrained alias of <see cref="IMethodSetup{TProperty}"/>
/// since property getter setup only needs <c>.Returns(value)</c>.
/// </summary>
public interface IPropertySetup<TProperty> : IMethodSetup<TProperty>;

/// <summary>
/// Fluent setup builder for property setters.
/// Returned by <c>mock.Setup.PropertyName.Setter</c> or <c>mock.Setup.PropertyName.Set(value)</c>.
/// </summary>
public interface IPropertySetterSetup : IVoidMethodSetup;
