using System.Runtime.CompilerServices;

namespace TUnit.Mocks;

/// <summary>
/// Generic fallback extension providing <c>T.Mock()</c> for any class or interface type.
/// Source-generated per-type extensions take precedence via C# 14 type specificity rules
/// and return a specialized wrapper type.
/// </summary>
public static class MockExtension
{
    extension<T>(T) where T : class
    {
        /// <summary>
        /// Creates a mock of <typeparamref name="T"/>.
        /// When a source-generated extension exists for this type, the compiler
        /// will prefer it over this generic fallback.
        /// </summary>
        [OverloadResolutionPriority(-1)]
        public static Mock<T> Mock(MockBehavior behavior = MockBehavior.Loose)
            => TUnit.Mocks.Mock.Of<T>(behavior);
    }
}
