namespace TUnit.Mocks.Setup.Behaviors;

internal sealed class CompositeBehavior : IBehavior
{
    private readonly IBehavior[] _behaviors;

    private CompositeBehavior(IBehavior[] behaviors) => _behaviors = behaviors;

    public static IBehavior Combine(IBehavior first, IBehavior second)
    {
        if (first is CompositeBehavior composite)
        {
            var combined = new IBehavior[composite._behaviors.Length + 1];
            composite._behaviors.CopyTo(combined, 0);
            combined[^1] = second;
            return new CompositeBehavior(combined);
        }

        return new CompositeBehavior([first, second]);
    }

    public object? Execute(object?[] arguments)
    {
        object? result = null;

        foreach (var behavior in _behaviors)
        {
            var behaviorResult = behavior is IArgumentFreeBehavior argumentFree
                ? argumentFree.Execute()
                : behavior.Execute(arguments);

            if (behavior is not ISideEffectBehavior)
            {
                result = behaviorResult;
            }
        }

        return result;
    }

    internal object? Execute<T1>(T1 arg1)
    {
        object? result = null;
        object?[]? arguments = null;

        foreach (var behavior in _behaviors)
        {
            var behaviorResult = behavior switch
            {
                IArgumentFreeBehavior argumentFree => argumentFree.Execute(),
                ITypedBehavior<T1> typed => typed.Execute(arg1),
                _ => behavior.Execute(arguments ??= [arg1])
            };

            if (behavior is not ISideEffectBehavior)
            {
                result = behaviorResult;
            }
        }

        return result;
    }

    internal object? Execute<T1, T2>(T1 arg1, T2 arg2)
    {
        object? result = null;
        object?[]? arguments = null;

        foreach (var behavior in _behaviors)
        {
            var behaviorResult = behavior switch
            {
                IArgumentFreeBehavior argumentFree => argumentFree.Execute(),
                ITypedBehavior<T1, T2> typed => typed.Execute(arg1, arg2),
                _ => behavior.Execute(arguments ??= [arg1, arg2])
            };

            if (behavior is not ISideEffectBehavior)
            {
                result = behaviorResult;
            }
        }

        return result;
    }

    internal object? Execute<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
    {
        object? result = null;
        object?[]? arguments = null;

        foreach (var behavior in _behaviors)
        {
            var behaviorResult = behavior switch
            {
                IArgumentFreeBehavior argumentFree => argumentFree.Execute(),
                ITypedBehavior<T1, T2, T3> typed => typed.Execute(arg1, arg2, arg3),
                _ => behavior.Execute(arguments ??= [arg1, arg2, arg3])
            };

            if (behavior is not ISideEffectBehavior)
            {
                result = behaviorResult;
            }
        }

        return result;
    }

    internal object? Execute<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        object? result = null;
        object?[]? arguments = null;

        foreach (var behavior in _behaviors)
        {
            var behaviorResult = behavior switch
            {
                IArgumentFreeBehavior argumentFree => argumentFree.Execute(),
                ITypedBehavior<T1, T2, T3, T4> typed => typed.Execute(arg1, arg2, arg3, arg4),
                _ => behavior.Execute(arguments ??= [arg1, arg2, arg3, arg4])
            };

            if (behavior is not ISideEffectBehavior)
            {
                result = behaviorResult;
            }
        }

        return result;
    }

    internal object? Execute<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        object? result = null;
        object?[]? arguments = null;

        foreach (var behavior in _behaviors)
        {
            var behaviorResult = behavior switch
            {
                IArgumentFreeBehavior argumentFree => argumentFree.Execute(),
                ITypedBehavior<T1, T2, T3, T4, T5> typed => typed.Execute(arg1, arg2, arg3, arg4, arg5),
                _ => behavior.Execute(arguments ??= [arg1, arg2, arg3, arg4, arg5])
            };

            if (behavior is not ISideEffectBehavior)
            {
                result = behaviorResult;
            }
        }

        return result;
    }

    internal object? Execute<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        object? result = null;
        object?[]? arguments = null;

        foreach (var behavior in _behaviors)
        {
            var behaviorResult = behavior switch
            {
                IArgumentFreeBehavior argumentFree => argumentFree.Execute(),
                ITypedBehavior<T1, T2, T3, T4, T5, T6> typed => typed.Execute(arg1, arg2, arg3, arg4, arg5, arg6),
                _ => behavior.Execute(arguments ??= [arg1, arg2, arg3, arg4, arg5, arg6])
            };

            if (behavior is not ISideEffectBehavior)
            {
                result = behaviorResult;
            }
        }

        return result;
    }

    internal object? Execute<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        object? result = null;
        object?[]? arguments = null;

        foreach (var behavior in _behaviors)
        {
            var behaviorResult = behavior switch
            {
                IArgumentFreeBehavior argumentFree => argumentFree.Execute(),
                ITypedBehavior<T1, T2, T3, T4, T5, T6, T7> typed => typed.Execute(arg1, arg2, arg3, arg4, arg5, arg6, arg7),
                _ => behavior.Execute(arguments ??= [arg1, arg2, arg3, arg4, arg5, arg6, arg7])
            };

            if (behavior is not ISideEffectBehavior)
            {
                result = behaviorResult;
            }
        }

        return result;
    }

    internal object? Execute<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        object? result = null;
        object?[]? arguments = null;

        foreach (var behavior in _behaviors)
        {
            var behaviorResult = behavior switch
            {
                IArgumentFreeBehavior argumentFree => argumentFree.Execute(),
                ITypedBehavior<T1, T2, T3, T4, T5, T6, T7, T8> typed => typed.Execute(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8),
                _ => behavior.Execute(arguments ??= [arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8])
            };

            if (behavior is not ISideEffectBehavior)
            {
                result = behaviorResult;
            }
        }

        return result;
    }
}
