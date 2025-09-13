#!/usr/bin/env python3

import re

# Read the file
with open('/home/runner/work/TUnit/TUnit/TUnit.Engine/Services/HookCollectionService.cs', 'r') as f:
    content = f.read()

# Fix all the remaining async patterns

# 1. Fix BuildAfterTestHooksAsync method declaration
content = re.sub(
    r'private async Task<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> BuildAfterTestHooksAsync\(Type type\)',
    r'private IReadOnlyList<Func<TestContext, CancellationToken, Task>> BuildAfterTestHooks(Type type)',
    content
)

# 2. Fix BuildBeforeEveryTestHooksAsync method declaration  
content = re.sub(
    r'private async Task<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> BuildBeforeEveryTestHooksAsync\(Type type\)',
    r'private IReadOnlyList<Func<TestContext, CancellationToken, Task>> BuildBeforeEveryTestHooks(Type type)',
    content
)

# 3. Fix BuildAfterEveryTestHooksAsync method declaration
content = re.sub(
    r'private async Task<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> BuildAfterEveryTestHooksAsync\(Type type\)',
    r'private IReadOnlyList<Func<TestContext, CancellationToken, Task>> BuildAfterEveryTestHooks(Type type)',
    content
)

# 4. Fix BuildBeforeClassHooksAsync method declaration
content = re.sub(
    r'private async Task<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> BuildBeforeClassHooksAsync\(Type type\)',
    r'private IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>> BuildBeforeClassHooks(Type type)',
    content
)

# 5. Fix BuildAfterClassHooks method declaration (already renamed)
content = re.sub(
    r'private async Task<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> BuildAfterClassHooks\(Type type\)',
    r'private IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>> BuildAfterClassHooks(Type type)',
    content
)

# 6. Fix await calls in build methods - replace all await calls with .Result
content = re.sub(
    r'var hookFunc = await (\w+)\(hook\);',
    r'var hookFunc = \1(hook).Result;',
    content
)

# 7. Fix all collection methods to be synchronous
# CollectAfterTestHooksAsync
content = re.sub(
    r'public async ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectAfterTestHooksAsync\(Type testClassType\)\s*{\s*if \(_afterTestHooksCache\.TryGetValue\(testClassType, out var cachedHooks\)\)\s*{\s*return cachedHooks;\s*}\s*var hooks = await BuildAfterTestHooksAsync\(testClassType\);',
    r'public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectAfterTestHooksAsync(Type testClassType)\n    {\n        if (_afterTestHooksCache.TryGetValue(testClassType, out var cachedHooks))\n        {\n            return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(cachedHooks);\n        }\n\n        var hooks = BuildAfterTestHooks(testClassType);',
    content,
    flags=re.DOTALL
)

# Write the file back
with open('/home/runner/work/TUnit/TUnit/TUnit.Engine/Services/HookCollectionService.cs', 'w') as f:
    f.write(content)

print("Fixed remaining async patterns in HookCollectionService.cs")