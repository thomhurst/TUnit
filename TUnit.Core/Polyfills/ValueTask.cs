using System.Runtime.CompilerServices;

#if NETSTANDARD2_0
// ReSharper disable once CheckNamespace
namespace System.Threading.Tasks;

public class ValueTask : Task
{
    public ValueTask(Action action) : base(action)
    {
    }
}
#endif