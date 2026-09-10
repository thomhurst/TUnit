namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class HookRegistrationIndices
{
    private static int _beforeTestHookIndex;
    private static int _afterTestHookIndex;
    private static int _beforeEveryTestHookIndex;
    private static int _afterEveryTestHookIndex;
    private static int _beforeClassHookIndex;
    private static int _afterClassHookIndex;
    private static int _beforeEveryClassHookIndex;
    private static int _afterEveryClassHookIndex;
    private static int _beforeAssemblyHookIndex;
    private static int _afterAssemblyHookIndex;
    private static int _beforeEveryAssemblyHookIndex;
    private static int _afterEveryAssemblyHookIndex;
    private static int _beforeTestSessionHookIndex;
    private static int _afterTestSessionHookIndex;
    private static int _beforeTestDiscoveryHookIndex;
    private static int _afterTestDiscoveryHookIndex;

    public static int GetNextBeforeTestHookIndex() => Interlocked.Increment(ref _beforeTestHookIndex);
    public static int GetNextAfterTestHookIndex() => Interlocked.Increment(ref _afterTestHookIndex);
    public static int GetNextBeforeEveryTestHookIndex() => Interlocked.Increment(ref _beforeEveryTestHookIndex);
    public static int GetNextAfterEveryTestHookIndex() => Interlocked.Increment(ref _afterEveryTestHookIndex);
    public static int GetNextBeforeClassHookIndex() => Interlocked.Increment(ref _beforeClassHookIndex);
    public static int GetNextAfterClassHookIndex() => Interlocked.Increment(ref _afterClassHookIndex);
    public static int GetNextBeforeEveryClassHookIndex() => Interlocked.Increment(ref _beforeEveryClassHookIndex);
    public static int GetNextAfterEveryClassHookIndex() => Interlocked.Increment(ref _afterEveryClassHookIndex);
    public static int GetNextBeforeAssemblyHookIndex() => Interlocked.Increment(ref _beforeAssemblyHookIndex);
    public static int GetNextAfterAssemblyHookIndex() => Interlocked.Increment(ref _afterAssemblyHookIndex);
    public static int GetNextBeforeEveryAssemblyHookIndex() => Interlocked.Increment(ref _beforeEveryAssemblyHookIndex);
    public static int GetNextAfterEveryAssemblyHookIndex() => Interlocked.Increment(ref _afterEveryAssemblyHookIndex);
    public static int GetNextBeforeTestSessionHookIndex() => Interlocked.Increment(ref _beforeTestSessionHookIndex);
    public static int GetNextAfterTestSessionHookIndex() => Interlocked.Increment(ref _afterTestSessionHookIndex);
    public static int GetNextBeforeTestDiscoveryHookIndex() => Interlocked.Increment(ref _beforeTestDiscoveryHookIndex);
    public static int GetNextAfterTestDiscoveryHookIndex() => Interlocked.Increment(ref _afterTestDiscoveryHookIndex);
}