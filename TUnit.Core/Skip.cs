using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Exceptions;

namespace TUnit.Core
{
    public static class Skip
    {
        /// <summary>
        /// Dynamically skips the current test.
        /// <para/>
        /// This is used, when the decision whether a test should be skipped happens at runtime rather than at discovery time.
        /// Otherwise consider using the <see cref="SkipAttribute"/> instead.
        /// </summary>
        /// <param name="reason">The reason why the test was skipped</param>
        [DoesNotReturn]
        public static void Test(string reason)
        {
            throw new SkipTestException(reason);
        }

        /// <summary>
        /// Dynamically skips the current test when the <paramref name="condition"/> is <c>false</c>.
        /// </summary>
        /// <param name="condition">When <c>false</c>, the test will be skipped; otherwise it will continue to run</param>
        /// <param name="reason">The reason why the test was skipped</param>
        public static void Unless([DoesNotReturnIf(false)] bool condition, string reason)
        {
            if (!condition)
            {
                Test(reason);
            }
        }

        /// <summary>
        /// Dynamically skips the current test when the <paramref name="condition"/> is <c>true</c>.
        /// </summary>
        /// <param name="condition">When <c>true</c>, the test will be skipped; otherwise it will continue to run</param>
        /// <param name="reason">The reason why the test was skipped</param>
        public static void When([DoesNotReturnIf(true)] bool condition, string reason)
        {
            if (condition)
            {
                Test(reason);
            }
        }
    }
}
