using System.Runtime.CompilerServices;

#if !NET8_0_OR_GREATER
// ReSharper disable once CheckNamespace
namespace System.Threading.Tasks;

public class ValueTask : Task
{
    public ValueTask(Action action) : base(action)
    {
    }
}
#endif