using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Represents an asynchronous event that can have multiple ordered handlers.
/// </summary>
/// <typeparam name="TEventArgs">The type of event arguments passed to handlers.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="AsyncEvent{TEventArgs}"/> class provides a mechanism for managing and invoking
/// asynchronous event handlers with support for execution ordering. Handlers can be added with
/// specific order values, and they will be invoked in ascending order.
/// </para>
/// <para>
/// This class is particularly useful in test frameworks where the order of event handler execution
/// matters, such as test lifecycle hooks (Before/After events) where setup and teardown operations
/// need to occur in a specific sequence.
/// </para>
/// <para>
/// Handlers are stored in a sorted list based on their order value. Lower order values execute first.
/// The default order is <c>int.MaxValue / 2</c>, allowing for both lower and higher priority handlers.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var testEvent = new AsyncEvent&lt;TestEventArgs&gt;();
///
/// // Add handlers with different priorities
/// testEvent.Add(async (sender, args) =>
/// {
///     await Task.Delay(100);
///     Console.WriteLine("First handler (order 1)");
/// }, order: 1);
///
/// testEvent.Add(async (sender, args) =>
/// {
///     Console.WriteLine("Second handler (order 10)");
/// }, order: 10);
///
/// // Or use operator overload with default order
/// testEvent += async (sender, args) =>
/// {
///     Console.WriteLine("Default priority handler");
/// };
/// </code>
/// </example>
public class AsyncEvent<TEventArgs>
{
    private List<Invocation>? _handlers;

    /// <summary>
    /// Represents a single invocation of an async event handler.
    /// </summary>
    /// <param name="factory">The async function to invoke when the event is raised.</param>
    /// <param name="order">The execution order for this handler.</param>
    public class Invocation(Func<object, TEventArgs, ValueTask> factory, int order) : IEventReceiver
    {
        /// <summary>
        /// Gets the execution order for this invocation.
        /// </summary>
        /// <value>Lower values execute first.</value>
        public int Order { get; } = order;

        /// <summary>
        /// Invokes the async event handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="eventArgs">The event arguments.</param>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
        public async ValueTask InvokeAsync(object sender, TEventArgs eventArgs)
        {
            await factory(sender, eventArgs);
        }
    }

    /// <summary>
    /// Adds an event handler with a specified execution order.
    /// </summary>
    /// <param name="callback">The async function to invoke when the event is raised.</param>
    /// <param name="order">The execution order for this handler. Lower values execute first. Default is <c>int.MaxValue / 2</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is null.</exception>
    /// <remarks>
    /// Handlers are automatically sorted by their order value. If multiple handlers have the same order,
    /// they will execute in the order they were added.
    /// </remarks>
    public void Add(Func<object, TEventArgs, ValueTask> callback, int order = int.MaxValue / 2)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        var invocation = new Invocation(callback, order);
        var insertIndex = FindInsertionIndex(order);
        (_handlers ??= []).Insert(insertIndex, invocation);
    }

    /// <summary>
    /// Adds an event handler at a specific position in the invocation list.
    /// </summary>
    /// <param name="callback">The async function to invoke when the event is raised.</param>
    /// <param name="index">The zero-based index at which to insert the handler. Values outside the valid range are clamped.</param>
    /// <param name="order">The execution order for this handler. Default is <c>int.MaxValue / 2</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is null.</exception>
    /// <remarks>
    /// If <paramref name="index"/> is negative, the handler is inserted at index 0.
    /// If <paramref name="index"/> exceeds the current count, the handler is appended to the end.
    /// </remarks>
    public void AddAt(Func<object, TEventArgs, ValueTask> callback, int index, int order = int.MaxValue / 2)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        var invocation = new Invocation(callback, order);
        var handlers = _handlers ??= [];
        var clampedIndex = index < 0 ? 0 : (index > handlers.Count ? handlers.Count : index);
        handlers.Insert(clampedIndex, invocation);
    }

    /// <summary>
    /// Gets a read-only list of all registered event handlers in execution order.
    /// </summary>
    /// <value>
    /// A read-only list of <see cref="Invocation"/> objects, sorted by their <see cref="Invocation.Order"/> value.
    /// Returns an empty list if no handlers are registered.
    /// </value>
    public IReadOnlyList<Invocation> InvocationList
    {
        get
        {
            if (_handlers == null)
            {
                return [];
            }

            return _handlers;

        }
    }

    /// <summary>
    /// Adds an event handler at the front of the invocation list, ensuring it executes before all other handlers.
    /// </summary>
    /// <param name="callback">The async function to invoke when the event is raised.</param>
    /// <returns>The current <see cref="AsyncEvent{TEventArgs}"/> instance for method chaining.</returns>
    /// <remarks>
    /// This is a convenience method equivalent to calling <see cref="AddAt"/> with index 0.
    /// Useful for adding high-priority handlers that must execute first.
    /// </remarks>
    public AsyncEvent<TEventArgs> InsertAtFront(Func<object, TEventArgs, ValueTask> callback)
    {
        AddAt(callback, 0);
        return this;
    }

    /// <summary>
    /// Adds an event handler using the += operator with default order.
    /// </summary>
    /// <param name="e">The event to add the handler to. Can be null, in which case a new event is created.</param>
    /// <param name="callback">The async function to invoke when the event is raised.</param>
    /// <returns>The <see cref="AsyncEvent{TEventArgs}"/> instance with the handler added.</returns>
    /// <remarks>
    /// This operator provides a familiar syntax for adding event handlers, similar to standard .NET events.
    /// The handler is added with the default order value of <c>int.MaxValue / 2</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// AsyncEvent&lt;TestEventArgs&gt; testEvent = null;
    /// testEvent += async (sender, args) => { await DoSomethingAsync(); };
    /// </code>
    /// </example>
    public static AsyncEvent<TEventArgs> operator +(
        AsyncEvent<TEventArgs>? e, Func<object, TEventArgs, ValueTask> callback)
    {
        e ??= new AsyncEvent<TEventArgs>();
        e.Add(callback);
        return e;
    }

    private int FindInsertionIndex(int order)
    {
        int left = 0, right = (_handlers ??= []).Count;
        while (left < right)
        {
            var mid = left + (right - left) / 2;
            if (_handlers[mid].Order <= order)
                left = mid + 1;
            else
                right = mid;
        }
        return left;
    }
}
