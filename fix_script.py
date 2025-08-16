#!/usr/bin/env python3

import re

# Read the file
with open('/home/runner/work/TUnit/TUnit/TUnit.Engine/Services/HookCollectionService.cs', 'r') as f:
    content = f.read()

# Fix the ProcessHookRegistration method to be async (already done manually)

# Pattern to fix ValueTask collection methods to be async
collection_pattern = r'public ValueTask<(.+?)> (Collect\w+HooksAsync)\([^)]+\)\s*\{\s*if \([^}]+\)\s*\{\s*return new ValueTask<[^>]+>\(cachedHooks\);\s*\}\s*var hooks = (Build\w+Hooks)\(([^)]+)\);\s*[^}]+\.TryAdd\([^)]+, hooks\);\s*return new ValueTask<[^>]+>\(hooks\);\s*\}'
collection_replacement = r'''public async ValueTask<\1> \2(\4)
    {
        if (_\2Cache.TryGetValue(\4, out var cachedHooks))
        {
            return cachedHooks;
        }

        var hooks = await \3Async(\4);
        _\2Cache.TryAdd(\4, hooks);
        return hooks;
    }'''

# Apply collection method fixes
content = re.sub(collection_pattern, collection_replacement, content, flags=re.DOTALL)

# Fix Build methods to be async
build_pattern = r'private (IReadOnlyList<[^>]+>) (Build\w+Hooks)\('
build_replacement = r'private async Task<\1> \2Async('
content = re.sub(build_pattern, build_replacement, content)

# Fix CreateXXXHookDelegate methods to be async
delegate_pattern = r'private (Func<[^>]+>) (Create\w+HookDelegate)\(([^)]+)\)\s*\{\s*// Process hook registration event receivers to handle skip attributes\s*ProcessHookRegistration\(([^)]+)\);'
delegate_replacement = r'private async Task<\1> \2Async(\3)\n    {\n        // Process hook registration event receivers to handle skip attributes\n        await ProcessHookRegistrationAsync(\4);'
content = re.sub(delegate_pattern, delegate_replacement, content)

# Fix calls to CreateXXXHookDelegate to be async
call_pattern = r'var hookFunc = (Create\w+HookDelegate)\(([^)]+)\);'
call_replacement = r'var hookFunc = await \1Async(\2);'
content = re.sub(call_pattern, call_replacement, content)

# Write the fixed content back
with open('/home/runner/work/TUnit/TUnit/TUnit.Engine/Services/HookCollectionService.cs', 'w') as f:
    f.write(content)

print("Fixed async patterns in HookCollectionService.cs")