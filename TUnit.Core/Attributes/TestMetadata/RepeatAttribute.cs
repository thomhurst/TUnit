namespace TUnit.Core;

/// <summary>
/// Specifies that a test method, test class, or assembly should be repeated a specified number of times.
/// </summary>
/// <remarks>
/// The RepeatAttribute causes the test to be executed the specified number of times.
/// This is useful for testing consistency across multiple executions or for stress testing.
///
/// Repeat can be applied at the method, class, or assembly level.
/// When applied at a class level, all test methods in the class will be repeated.
/// When applied at the assembly level, it affects all tests in the assembly.
///
/// Method-level attributes take precedence over class-level attributes, which take precedence over assembly-level attributes.
/// </remarks>
/// <example>
/// <code>
/// [Test]
/// [Repeat(5)]
/// public void TestThatShouldBeConsistent()
/// {
///     // This test will run 5 times
///     Assert.That(MyFunction()).IsTrue();
/// }
/// 
/// [Test]
/// [Repeat(100)]
/// public void StressTest()
/// {
///     // /Run this test 100 times to ensure reliability under load
///     var result = ComplexOperation();
///     Assert.That(result).IsValid();
/// }
/// </code>
/// </example>
// Don't think there's a way to enable inheritance on this because the source generator needs to access the constructor argument
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public sealed class RepeatAttribute : TUnitAttribute, IScopedAttribute<RepeatAttribute>
{
    /// <summary>
    /// Gets the number of times the test should be repeated.
    /// </summary>
    public int Times { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepeatAttribute"/> class with the specified number of repetitions.
    /// </summary>
    /// <param name="times">The number of times to repeat the test. Must be a non-negative integer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="times"/> is less than 0.</exception>
    public RepeatAttribute(int times)
    {
        if (times < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(times), "Repeat times must be positive");
        }

        Times = times;
    }
}
