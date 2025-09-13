using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Exception-specific assertions
[CreateAssertion<Exception>( typeof(ExceptionAssertionExtensions), nameof(HasInnerException))]
[CreateAssertion<Exception>( typeof(ExceptionAssertionExtensions), nameof(HasInnerException), CustomName = "HasNoInnerException", NegateLogic = true)]

[CreateAssertion<Exception>( typeof(ExceptionAssertionExtensions), nameof(HasStackTrace))]
[CreateAssertion<Exception>( typeof(ExceptionAssertionExtensions), nameof(HasStackTrace), CustomName = "HasNoStackTrace", NegateLogic = true)]

[CreateAssertion<Exception>( typeof(ExceptionAssertionExtensions), nameof(HasData))]
[CreateAssertion<Exception>( typeof(ExceptionAssertionExtensions), nameof(HasData), CustomName = "HasNoData", NegateLogic = true)]

[CreateAssertion<Exception>( typeof(ExceptionAssertionExtensions), nameof(HasHelpLink))]
[CreateAssertion<Exception>( typeof(ExceptionAssertionExtensions), nameof(HasHelpLink), CustomName = "HasNoHelpLink", NegateLogic = true)]

[CreateAssertion<AggregateException>( typeof(ExceptionAssertionExtensions), nameof(HasMultipleInnerExceptions))]
[CreateAssertion<AggregateException>( typeof(ExceptionAssertionExtensions), nameof(HasMultipleInnerExceptions), CustomName = "HasSingleInnerException", NegateLogic = true)]
public static partial class ExceptionAssertionExtensions
{
    internal static bool HasInnerException(Exception exception) => exception.InnerException != null;
    internal static bool HasStackTrace(Exception exception) => !string.IsNullOrEmpty(exception.StackTrace);
    internal static bool HasData(Exception exception) => exception.Data.Count > 0;
    internal static bool HasHelpLink(Exception exception) => !string.IsNullOrEmpty(exception.HelpLink);
    internal static bool HasMultipleInnerExceptions(AggregateException exception) => exception.InnerExceptions.Count > 1;
}