using System.Diagnostics.CodeAnalysis;
using System.Reflection;
    using TUnit.Core;
    using TUnit.Core.Data;
    using TUnit.Core.Hooks;
    
    namespace TUnit.Engine.Services;
    
    internal abstract class HooksCollectorBase(string sessionId)
    {
        public string SessionId { get; } = sessionId;
    
        [field: AllowNull, MaybeNull]
        internal List<StaticHookMethod<BeforeTestDiscoveryContext>> BeforeTestDiscoveryHooks
            => field ??= CollectBeforeTestDiscoveryHooks();
    
        [field: AllowNull, MaybeNull]
        internal List<StaticHookMethod<TestSessionContext>> BeforeTestSessionHooks
            => field ??= CollectBeforeTestSessionHooks();
    
        [field: AllowNull, MaybeNull]
        internal GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>> BeforeAssemblyHooks
            => field ??= CollectBeforeAssemblyHooks();

        [field: AllowNull, MaybeNull]
        internal GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>> BeforeClassHooks
            => field ??= CollectBeforeClassHooks();
    
        [field: AllowNull, MaybeNull]
        internal GetOnlyDictionary<Type, List<InstanceHookMethod>> BeforeTestHooks
            => field ??= CollectBeforeTestHooks();
    
        [field: AllowNull, MaybeNull]
        internal List<StaticHookMethod<AssemblyHookContext>> BeforeEveryAssemblyHooks
            => field ??= CollectBeforeEveryAssemblyHooks();
    
        [field: AllowNull, MaybeNull]
        internal List<StaticHookMethod<ClassHookContext>> BeforeEveryClassHooks
            => field ??= CollectBeforeEveryClassHooks();
    
        [field: AllowNull, MaybeNull]
        internal List<StaticHookMethod<TestContext>> BeforeEveryTestHooks
            => field ??= CollectBeforeEveryTestHooks();
    
        [field: AllowNull, MaybeNull]
        internal List<StaticHookMethod<TestDiscoveryContext>> AfterTestDiscoveryHooks
            => field ??= CollectAfterTestDiscoveryHooks();
    
        [field: AllowNull, MaybeNull]
        internal List<StaticHookMethod<TestSessionContext>> AfterTestSessionHooks
            => field ??= CollectAfterTestSessionHooks();
    
        [field: AllowNull, MaybeNull]
        internal GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>> AfterAssemblyHooks
            => field ??= CollectAfterAssemblyHooks();
    
        [field: AllowNull, MaybeNull]
        internal GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>> AfterClassHooks
            => field ??= CollectAfterClassHooks();
    
        [field: AllowNull, MaybeNull]
        internal GetOnlyDictionary<Type, List<InstanceHookMethod>> AfterTestHooks
            => field ??= CollectAfterTestHooks();
    
        [field: AllowNull, MaybeNull]
        internal List<StaticHookMethod<AssemblyHookContext>> AfterEveryAssemblyHooks
            => field ??= CollectAfterEveryAssemblyHooks();
    
        [field: AllowNull, MaybeNull]
        internal List<StaticHookMethod<ClassHookContext>> AfterEveryClassHooks
            => field ??= CollectAfterEveryClassHooks();
    
        [field: AllowNull, MaybeNull]
        internal List<StaticHookMethod<TestContext>> AfterEveryTestHooks
            => field ??= CollectAfterEveryTestHooks();
    
        protected private abstract List<StaticHookMethod<BeforeTestDiscoveryContext>> CollectBeforeTestDiscoveryHooks();
        protected private abstract List<StaticHookMethod<TestSessionContext>> CollectBeforeTestSessionHooks();
        protected private abstract GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>> CollectBeforeAssemblyHooks();
        protected private abstract GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>> CollectBeforeClassHooks();
        protected private abstract GetOnlyDictionary<Type, List<InstanceHookMethod>> CollectBeforeTestHooks();
        protected private abstract List<StaticHookMethod<AssemblyHookContext>> CollectBeforeEveryAssemblyHooks();
        protected private abstract List<StaticHookMethod<ClassHookContext>> CollectBeforeEveryClassHooks();
        protected private abstract List<StaticHookMethod<TestContext>> CollectBeforeEveryTestHooks();
        protected private abstract List<StaticHookMethod<TestDiscoveryContext>> CollectAfterTestDiscoveryHooks();
        protected private abstract List<StaticHookMethod<TestSessionContext>> CollectAfterTestSessionHooks();
        protected private abstract GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>> CollectAfterAssemblyHooks();
        protected private abstract GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>> CollectAfterClassHooks();
        protected private abstract GetOnlyDictionary<Type, List<InstanceHookMethod>> CollectAfterTestHooks();
        protected private abstract List<StaticHookMethod<AssemblyHookContext>> CollectAfterEveryAssemblyHooks();
        protected private abstract List<StaticHookMethod<ClassHookContext>> CollectAfterEveryClassHooks();
        protected private abstract List<StaticHookMethod<TestContext>> CollectAfterEveryTestHooks();
    }

