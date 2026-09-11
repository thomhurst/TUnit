using System.Text;

namespace TUnit.Engine.Exceptions;

/// <summary>
/// Folds an exception's inner-exception chain into <see cref="Exception.Message"/> and
/// <see cref="Exception.StackTrace"/> so that consumers which read only those two members still
/// see the whole chain.
/// </summary>
/// <remarks>
/// <para>
/// Microsoft.Testing.Platform's server-mode (IDE) serializer sends a failed test node as
/// <c>error.message</c> (the explanation or <see cref="Exception.Message"/>) and
/// <c>error.stacktrace</c> (<see cref="Exception.StackTrace"/>). It never walks
/// <see cref="Exception.InnerException"/>, so Rider and Visual Studio showed only the outermost
/// exception (https://github.com/thomhurst/TUnit/issues/1327). The console and <c>dotnet test</c>
/// paths flatten the chain themselves, which is why the CLI was never affected.
/// </para>
/// <para>
/// <see cref="Exception.InnerException"/> is deliberately <see langword="null"/> so that consumers
/// which do walk the chain do not print the inner exceptions twice. The original exception remains
/// reachable through <see cref="TUnitFailedException.WrappedException"/> for reporters that want the
/// structured chain (<see cref="TUnitFailedException.Unwrap"/>).
/// </para>
/// </remarks>
internal sealed class FlattenedException : TUnitFailedException
{
    private const string InnerMessagePrefix = " ---> ";
    private const string InnerStackTracePrefix = "--- Inner exception stack trace (";
    private const string InnerStackTraceSuffix = ") ---";

    private FlattenedException(Exception exception)
        : base(CombineMessages(exception), CombineStackTraces(exception), exception)
    {
    }

    /// <summary>
    /// Returns <paramref name="exception"/> unchanged when it has no inner exception, otherwise a
    /// <see cref="FlattenedException"/> presenting the whole chain.
    /// </summary>
    public static Exception Wrap(Exception exception)
    {
        return exception.InnerException is null
            ? exception
            : new FlattenedException(exception);
    }

    public override string ToString()
    {
        var type = WrappedException?.GetType().FullName ?? GetType().FullName;

        return StackTrace.Length == 0
            ? $"{type}: {Message}"
            : $"{type}: {Message}{Environment.NewLine}{StackTrace}";
    }

    /// <summary>
    /// The outer message followed by one <c> ---> Type: Message</c> line per inner exception,
    /// outermost first. Every inner exception of an <see cref="AggregateException"/> is included.
    /// </summary>
    internal static string CombineMessages(Exception exception)
    {
        if (exception.InnerException is null)
        {
            return exception.Message;
        }

        var builder = new StringBuilder(exception.Message);
        AppendInnerMessages(builder, exception);

        return builder.ToString();
    }

    /// <summary>
    /// The outer stack trace followed by each inner exception's stack trace, each introduced by a
    /// separator line naming the inner exception type, outermost first.
    /// </summary>
    internal static string CombineStackTraces(Exception exception)
    {
        if (exception.InnerException is null)
        {
            return exception.StackTrace ?? string.Empty;
        }

        var builder = new StringBuilder(exception.StackTrace);
        AppendInnerStackTraces(builder, exception);

        return builder.ToString();
    }

    private static void AppendInnerMessages(StringBuilder builder, Exception exception)
    {
        foreach (var inner in GetInnerExceptions(exception))
        {
            builder.Append(Environment.NewLine)
                .Append(InnerMessagePrefix)
                .Append(inner.GetType().FullName)
                .Append(": ")
                .Append(inner.Message);

            AppendInnerMessages(builder, inner);
        }
    }

    private static void AppendInnerStackTraces(StringBuilder builder, Exception exception)
    {
        foreach (var inner in GetInnerExceptions(exception))
        {
            if (builder.Length > 0)
            {
                builder.Append(Environment.NewLine);
            }

            builder.Append(InnerStackTracePrefix)
                .Append(inner.GetType().FullName)
                .Append(InnerStackTraceSuffix);

            if (!string.IsNullOrEmpty(inner.StackTrace))
            {
                builder.Append(Environment.NewLine).Append(inner.StackTrace);
            }

            AppendInnerStackTraces(builder, inner);
        }
    }

    // An AggregateException's InnerException is only its first member; enumerate them all so
    // sibling failures (e.g. from Task.WhenAll) are not silently dropped.
    private static IEnumerable<Exception> GetInnerExceptions(Exception exception)
    {
        if (exception is AggregateException aggregate)
        {
            return aggregate.InnerExceptions;
        }

        return exception.InnerException is { } inner
            ? [inner]
            : [];
    }
}
