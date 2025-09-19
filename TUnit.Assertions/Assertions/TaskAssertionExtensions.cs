using System.Threading.Tasks;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Task assertions
[CreateAssertion<Task>( nameof(Task.IsCompleted))]
[CreateAssertion<Task>( nameof(Task.IsCompleted), CustomName = "IsNotCompleted", NegateLogic = true)]

#if NET5_0_OR_GREATER
[CreateAssertion<Task>( nameof(Task.IsCompletedSuccessfully))]
[CreateAssertion<Task>( nameof(Task.IsCompletedSuccessfully), CustomName = "IsNotCompletedSuccessfully", NegateLogic = true)]
#endif

[CreateAssertion<Task>( nameof(Task.IsFaulted))]
[CreateAssertion<Task>( nameof(Task.IsFaulted), CustomName = "IsNotFaulted", NegateLogic = true)]

[CreateAssertion<Task>( nameof(Task.IsCanceled))]
[CreateAssertion<Task>( nameof(Task.IsCanceled), CustomName = "IsNotCanceled", NegateLogic = true)]
public static partial class TaskAssertionExtensions;