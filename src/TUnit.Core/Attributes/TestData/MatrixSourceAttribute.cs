using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Provides a way to specify discrete values for a test parameter.
/// When multiple parameters use the <see cref="MatrixAttribute"/>, TUnit will generate tests for all combinations.
/// </summary>
/// <remarks>
/// <para>
/// The Matrix attribute creates a combinatorial test matrix when used on multiple parameters.
/// Each parameter decorated with <see cref="MatrixAttribute"/> contributes its values to the combinations.
/// </para>
/// <para>
/// Example:
/// <code>
/// [Test]
/// public void MatrixTest(
///     [Matrix(1, 2)] int x,
///     [Matrix("a", "b")] string y)
/// {
///     // This will run 4 test cases:
///     // x=1, y="a"
///     // x=1, y="b"
///     // x=2, y="a"
///     // x=2, y="b"
/// }
/// </code>
/// </para>
/// <para>
/// You can exclude specific values from the matrix by using the <c>Excluding</c> property:
/// <code>
/// [Test]
/// public void MatrixWithExclusionsTest(
///     [Matrix(1, 2, 3) { Excluding = [3] }] int x,
///     [Matrix("a", "b")] string y)
/// {
///     // This will exclude combinations with x=3
/// }
/// </code>
/// </para>
/// </remarks>
/// <param name="objects">The values to be used for this parameter in the test matrix.</param>
[AttributeUsage(AttributeTargets.Parameter)]
public class MatrixAttribute(params object?[]? objects) : TUnitAttribute, IDataSourceMemberAttribute
{
    protected MatrixAttribute() : this(null)
    {
    }

    public virtual object?[] GetObjects(DataGeneratorMetadata dataGeneratorMetadata) => objects ?? [null];

    public object?[]? Excluding { get; init; }
}

/// <inheritdoc/>
[AttributeUsage(AttributeTargets.Parameter)]
public class MatrixAttribute<T>(params T?[]? objects) : MatrixAttribute(objects?.Cast<object>().ToArray()), IInfersType<T>;
