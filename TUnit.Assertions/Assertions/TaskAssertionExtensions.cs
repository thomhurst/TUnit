using System.Threading.Tasks;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Task assertions
[CreateAssertion(typeof(Task), nameof(Task.IsCompleted))]
[CreateAssertion(typeof(Task), nameof(Task.IsCompleted), CustomName = "IsNotCompleted", NegateLogic = true)]

#if NET5_0_OR_GREATER
[CreateAssertion(typeof(Task), nameof(Task.IsCompletedSuccessfully))]
[CreateAssertion(typeof(Task), nameof(Task.IsCompletedSuccessfully), CustomName = "IsNotCompletedSuccessfully", NegateLogic = true)]
#endif

[CreateAssertion(typeof(Task), nameof(Task.IsFaulted))]
[CreateAssertion(typeof(Task), nameof(Task.IsFaulted), CustomName = "IsNotFaulted", NegateLogic = true)]

[CreateAssertion(typeof(Task), nameof(Task.IsCanceled))]
[CreateAssertion(typeof(Task), nameof(Task.IsCanceled), CustomName = "IsNotCanceled", NegateLogic = true)]
public static partial class TaskAssertionExtensions;