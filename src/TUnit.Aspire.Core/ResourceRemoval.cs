using Aspire.Hosting.ApplicationModel;

namespace TUnit.Aspire;

/// <summary>
/// Removes resources from an Aspire app model and detaches the edges that kept resources still
/// hold on them. Removing a resource from the model alone leaves <c>WaitFor</c> and
/// <c>WithReference</c> edges behind: a kept resource then waits forever for a resource that never
/// starts, or blocks resolving an endpoint or connection string that is never allocated (#7284).
/// </summary>
internal static class ResourceRemoval
{
    // Bounds recursion through nested ReferenceExpressions; real value graphs are a few levels deep.
    private const int MaxReferenceDepth = 32;

    public static void Remove(IList<IResource> resources, IEnumerable<string> names, Action<string> log)
    {
        var removed = new HashSet<IResource>(ReferenceEqualityComparer.Instance);

        foreach (var name in names)
        {
            var resource = resources.SingleOrDefault(r => string.Equals(r.Name, name, StringComparison.Ordinal));

            if (resource is not null)
            {
                removed.Add(resource);
            }
            else
            {
                log($"ResourcesToRemove: resource '{name}' not found (skipped).");
            }
        }

        if (removed.Count == 0)
        {
            return;
        }

        AddOrphanedChildren(resources, removed, log);

        foreach (var resource in removed)
        {
            resources.Remove(resource);
        }

        foreach (var resource in resources)
        {
            DetachEdges(resource, removed, log);
        }
    }

    /// <summary>
    /// A child (for example a database on a removed server) cannot run without its parent, so it
    /// goes too. Repeats until stable so grandchildren follow.
    /// </summary>
    private static void AddOrphanedChildren(IList<IResource> resources, HashSet<IResource> removed, Action<string> log)
    {
        bool added;
        do
        {
            added = false;
            foreach (var resource in resources)
            {
                if (resource is IResourceWithParent child
                    && removed.Contains(child.Parent)
                    && removed.Add(resource))
                {
                    log($"ResourcesToRemove: also removing '{resource.Name}' because its parent '{child.Parent.Name}' is removed.");
                    added = true;
                }
            }
        } while (added);
    }

    private static void DetachEdges(IResource resource, HashSet<IResource> removed, Action<string> log)
    {
        var annotations = resource.Annotations;

        for (var i = annotations.Count - 1; i >= 0; i--)
        {
            switch (annotations[i])
            {
                case WaitAnnotation wait when removed.Contains(wait.Resource):
                    annotations.RemoveAt(i);
                    log($"ResourcesToRemove: dropped wait on removed resource '{wait.Resource.Name}' from '{resource.Name}'.");
                    break;
                case EndpointReferenceAnnotation endpointReference when removed.Contains(endpointReference.Resource):
                    annotations.RemoveAt(i);
                    log($"ResourcesToRemove: dropped endpoint reference to removed resource '{endpointReference.Resource.Name}' from '{resource.Name}'.");
                    break;
                case ResourceRelationshipAnnotation relationship when removed.Contains(relationship.Resource):
                    annotations.RemoveAt(i);
                    log($"ResourcesToRemove: dropped relationship to removed resource '{relationship.Resource.Name}' from '{resource.Name}'.");
                    break;
                case EnvironmentCallbackAnnotation environment:
                    annotations[i] = WrapEnvironmentCallback(environment, resource, removed, log);
                    break;
                case CommandLineArgsCallbackAnnotation args:
                    annotations[i] = WrapArgsCallback(args, resource, removed, log);
                    break;
            }
        }
    }

    /// <summary>
    /// <c>WithReference</c> and <c>WithEnvironment</c> callbacks capture the removed resource in a
    /// closure, so they cannot be detached by annotation. Run them, then drop each value that
    /// references a removed resource before Aspire tries to resolve it.
    /// </summary>
    private static EnvironmentCallbackAnnotation WrapEnvironmentCallback(
        EnvironmentCallbackAnnotation original, IResource resource, HashSet<IResource> removed, Action<string> log)
    {
        return new EnvironmentCallbackAnnotation(async context =>
        {
            await original.Callback(context).ConfigureAwait(false);

            List<string>? toRemove = null;
            foreach (var (key, value) in context.EnvironmentVariables)
            {
                if (ReferencesRemoved(value, removed, 0))
                {
                    (toRemove ??= []).Add(key);
                }
            }

            if (toRemove is null)
            {
                return;
            }

            foreach (var key in toRemove)
            {
                context.EnvironmentVariables.Remove(key);
                log($"ResourcesToRemove: dropped environment variable '{key}' from '{resource.Name}' because it references a removed resource.");
            }
        });
    }

    private static CommandLineArgsCallbackAnnotation WrapArgsCallback(
        CommandLineArgsCallbackAnnotation original, IResource resource, HashSet<IResource> removed, Action<string> log)
    {
        return new CommandLineArgsCallbackAnnotation(async context =>
        {
            // A callback can replace or rebuild earlier arguments, so the whole list is scanned. An option is
            // only paired with a value when this callback added it: an argument object that was in the list
            // before the callback ran belongs to an earlier WithArgs call, wherever it now sits.
            var before = new HashSet<object>(context.Args, ReferenceEqualityComparer.Instance);
            await original.Callback(context).ConfigureAwait(false);

            for (var i = context.Args.Count - 1; i >= 0; i--)
            {
                var argument = context.Args[i];
                if (!ReferencesRemoved(argument, removed, 0))
                {
                    continue;
                }

                context.Args.RemoveAt(i);

                // "--name value": dropping only the value would make the next argument the option's value.
                // A dropped "--name=value" is an option itself, so it is never the previous option's value,
                // and after a "--" separator every argument is positional.
                if (i > 0
                    && !before.Contains(context.Args[i - 1])
                    && !IsOption(argument)
                    && IsOptionName(context.Args[i - 1])
                    && !HasSeparatorBefore(context.Args, i - 1))
                {
                    var option = (string)context.Args[i - 1];
                    context.Args.RemoveAt(i - 1);
                    i--;
                    log($"ResourcesToRemove: dropped argument '{option}' and its value from '{resource.Name}' because the value references a removed resource.");
                }
                else
                {
                    log($"ResourcesToRemove: dropped argument {i} from '{resource.Name}' because it references a removed resource.");
                }
            }
        });
    }

    // "--url=value" is one argument and is dropped whole, so only a bare option name pairs with the next argument.
    private static bool IsOptionName(object? argument)
        => argument is string { Length: > 1 } text && text[0] == '-' && text != "--" && !text.Contains('=');

    private static bool HasSeparatorBefore(IList<object> args, int index)
    {
        for (var i = 0; i < index; i++)
        {
            if (args[i] is "--")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOption(object? argument) => argument switch
    {
        string text => text.StartsWith('-'),
        ReferenceExpression expression => expression.Format.StartsWith('-'),
        _ => false,
    };

    private static bool ReferencesRemoved(object? value, HashSet<IResource> removed, int depth)
    {
        switch (value)
        {
            // Stop at resources: a kept resource is valid even if it was built from other values.
            case IResource referenced:
                return removed.Contains(referenced);
            case ConnectionStringReference connectionString when removed.Contains(connectionString.Resource):
                return true;
            case IValueWithReferences withReferences when depth < MaxReferenceDepth:
                foreach (var reference in withReferences.References)
                {
                    if (ReferencesRemoved(reference, removed, depth + 1))
                    {
                        return true;
                    }
                }

                return false;
            default:
                return false;
        }
    }
}
