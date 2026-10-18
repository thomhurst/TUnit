namespace TUnit.Mocks;

/// <summary>
/// Optional capability of a class or wrap mock: whether unconfigured virtual members run the base implementation.
/// Kept separate from <see cref="IMockControl{T}"/> so adding it did not break existing implementers of that interface.
/// </summary>
public interface IMockPassThrough
{
    /// <summary>
    /// Whether unconfigured virtual members of a class or wrap mock run the base implementation (default true).
    /// When false they return default values (loose) or throw (strict), like interface members.
    /// Resetting a mock does not change it, like <see cref="IMockControl{T}.Behavior"/> and
    /// <see cref="IMockControl{T}.DefaultValueProvider"/>.
    /// </summary>
    bool PassThrough { get; set; }
}
