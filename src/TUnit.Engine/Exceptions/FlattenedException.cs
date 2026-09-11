using System.Collections;
using System.Text;
using TUnit.Core.Exceptions;

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
        // MTP's server-mode serializer reads Data["assert.expected"] / Data["assert.actual"] from the
        // reported exception as its expected/actual fallback, so keep the original entries reachable.
        foreach (DictionaryEntry entry in exception.Data)
        {
            Data[entry.Key] = entry.Value;
        }

        // Classification uses the first aggregate member. Preserve its assertion diff when the
        // aggregate itself has no expected/actual values, without copying unrelated sibling data.
        if (ChainSource(exception) is AggregateException { InnerExceptions.Count: > 0 } aggregate)
        {
            CopyAssertionData(aggregate.InnerExceptions[0], "assert.expected");
            CopyAssertionData(aggregate.InnerExceptions[0], "assert.actual");
        }
    }

    private void CopyAssertionData(Exception source, string key)
    {
        if (!Data.Contains(key) && source.Data.Contains(key))
        {
            Data[key] = source.Data[key];
        }
    }

    /// <summary>
    /// Returns <paramref name="exception"/> unchanged when it has no inner exception, otherwise a
    /// <see cref="FlattenedException"/> presenting the whole chain.
    /// </summary>
    public static Exception Wrap(Exception exception)
    {
        return ChainSource(exception).InnerException is null
            ? exception
            : new FlattenedException(exception);
    }

    public override string ToString()
    {
        var type = WrappedException!.GetType().FullName;

        return StackTrace.Length == 0
            ? $"{type}: {Message}"
            : $"{type}: {Message}{Environment.NewLine}{StackTrace}";
    }

    /// <summary>
    /// The outer message followed by one <c> ---> Type: Message</c> line per inner exception,
    /// outermost first. Every member of an <see cref="AggregateException"/> is included; an inner
    /// aggregate itself gets no line, and messages embedded by known TUnit wrappers are not repeated.
    /// </summary>
    internal static string CombineMessages(Exception exception)
    {
        var chainSource = ChainSource(exception);

        if (chainSource.InnerException is null)
        {
            return exception.Message;
        }

        var builder = new StringBuilder(exception.Message);
        AppendInnerMessages(builder, chainSource, EmbeddedMessage(chainSource));

        return builder.ToString();
    }

    /// <summary>
    /// The outer stack trace followed by each thrown inner exception's stack trace, each introduced
    /// by a separator line naming the inner exception type, outermost first.
    /// </summary>
    internal static string CombineStackTraces(Exception exception)
    {
        var chainSource = ChainSource(exception);

        if (chainSource.InnerException is null)
        {
            return exception.StackTrace ?? string.Empty;
        }

        // A TUnit wrapper presents a filtered outer trace (TUnit internals omitted); keep the inner
        // traces consistent with it instead of reintroducing engine frames below the hint.
        var filter = exception is TUnitFailedException;

        var builder = new StringBuilder(exception.StackTrace);
        AppendInnerStackTraces(builder, chainSource, filter);

        return builder.ToString();
    }

    // The console wrapper (TestFailedException) keeps only the first member of a wrapped
    // AggregateException as its InnerException, so the chain is folded from the wrapped original to
    // keep every sibling; the wrapper's own (filtered) Message and StackTrace still lead the output.
    // A FlattenedException is already folded: its InnerException is null, so nothing is appended.
    private static Exception ChainSource(Exception exception)
    {
        return exception is TUnitFailedException { WrappedException: { } wrapped } and not FlattenedException
            ? wrapped
            : exception;
    }

    // Only known TUnit wrappers embed their causes. Arbitrary user exceptions may contain the
    // same text by coincidence, so substring matches must never suppress their inner exceptions.
    private static string? EmbeddedMessage(Exception exception)
    {
        if (exception is BeforeTestException or BeforeClassException or BeforeAssemblyException
            or BeforeTestSessionException or BeforeTestDiscoveryException
            or AfterTestException or AfterClassException or AfterAssemblyException
            or AfterTestSessionException or AfterTestDiscoveryException
            || (exception.GetType().FullName == "TUnit.Assertions.Exceptions.AssertionException"
                && exception.InnerException is AggregateException))
        {
            return exception.Message;
        }

        return null;
    }

    // Keep the embedding wrapper's message across an inner aggregate, which only groups members.
    // Descendants are still visited even when a known wrapper already includes a member's message.
    private static void AppendInnerMessages(StringBuilder builder, Exception exception, string? ancestorMessage)
    {
        foreach (var inner in GetInnerExceptions(exception))
        {
            if (inner is AggregateException)
            {
                AppendInnerMessages(builder, inner, ancestorMessage);
                continue;
            }

            var message = inner.Message;

            if (message.Length == 0
                || ancestorMessage is null || ancestorMessage.IndexOf(message, StringComparison.Ordinal) < 0)
            {
                builder.Append(Environment.NewLine)
                    .Append(InnerMessagePrefix)
                    .Append(inner.GetType().FullName)
                    .Append(": ")
                    .Append(message);
            }

            AppendInnerMessages(builder, inner, EmbeddedMessage(inner));
        }
    }

    // An inner exception that was never thrown (an Assert.Multiple member, a hand-built cause) has
    // no frames; its type is already on the message line, so no separator is emitted for it.
    private static void AppendInnerStackTraces(StringBuilder builder, Exception exception, bool filter)
    {
        foreach (var inner in GetInnerExceptions(exception))
        {
            var stackTrace = inner.StackTrace;

            if (!string.IsNullOrEmpty(stackTrace))
            {
                if (builder.Length > 0)
                {
                    builder.Append(Environment.NewLine);
                }

                builder.Append(InnerStackTracePrefix)
                    .Append(inner.GetType().FullName)
                    .Append(InnerStackTraceSuffix)
                    .Append(Environment.NewLine)
                    .Append(filter ? FilterStackTrace(stackTrace) : stackTrace);
            }

            AppendInnerStackTraces(builder, inner, filter);
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
