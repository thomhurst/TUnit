import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { access, mkdtemp, readFile, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { delimiter, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const publisher = fileURLToPath(new URL('./publish-claude-review.mjs', import.meta.url));
const commentUrl = 'https://github.com/example/repository/pull/42#issuecomment-1234';
const success = result => ({
    type: 'result', subtype: 'success', is_error: false, result, permission_denials: [],
});

async function runPublisher(t, messages, options = {}) {
    const directory = await mkdtemp(join(tmpdir(), 'claude-review-'));
    t.after(() => rm(directory, { recursive: true, force: true }));
    const execution = join(directory, 'execution.json');
    const summary = join(directory, 'summary.md');
    const calls = join(directory, 'calls.jsonl');
    const fakeGh = join(directory, 'fake-gh.mjs');
    if (!options.missingExecution) {
        const content = typeof messages === 'function' ? messages(directory) : messages;
        await writeFile(execution, options.rawExecution ?? JSON.stringify(content));
    }
    await writeFile(summary, '');
    await writeFile(calls, '');
    await writeFile(fakeGh, `
import { appendFileSync } from 'node:fs';
appendFileSync(process.env.FAKE_GH_CALLS, JSON.stringify(process.argv.slice(2)) + '\\n');
console.log(process.env.FAKE_GH_OUTPUT);
console.error(process.env.FAKE_GH_STDERR);
process.exitCode = Number(process.env.FAKE_GH_EXIT);
`);
    await writeFile(join(directory, 'gh'), '#!/usr/bin/env bash\nexec node "$FAKE_GH_SCRIPT" "$@"\n', { mode: 0o755 });
    const result = spawnSync(process.execPath, [publisher], {
        encoding: 'utf8',
        timeout: 10000,
        env: {
            ...process.env,
            PATH: directory + delimiter + process.env.PATH,
            CLAUDE_EXECUTION_FILE: execution,
            CLAUDE_ACTION_OUTCOME: 'success',
            GH_REPO: 'example/repository',
            PR_NUMBER: '42',
            GITHUB_STEP_SUMMARY: summary,
            FAKE_GH_SCRIPT: fakeGh,
            FAKE_GH_CALLS: calls,
            FAKE_GH_OUTPUT: commentUrl,
            FAKE_GH_STDERR: '',
            FAKE_GH_EXIT: '0',
            ...options.env,
        },
    });
    assert.ifError(result.error);
    return {
        ...result,
        directory,
        summary: await readFile(summary, 'utf8'),
        calls: (await readFile(calls, 'utf8')).trim().split('\n').filter(Boolean).map(line => JSON.parse(line)),
    };
}

test('publishes final Markdown once to the triggering PR through the real helper', async t => {
    const markdown = '## Review\n\nFound an incorrect boundary check.';
    const result = await runPublisher(t, [success(markdown)]);
    assert.equal(result.status, 0, result.stderr);
    assert.deepEqual(result.calls, [['pr', 'comment', '42', '--repo', 'example/repository', '--body', markdown]]);
    assert.ok(result.summary.includes(markdown));
    assert.ok(result.summary.includes(commentUrl));
});

for (const [name, markdown] of [
    ['ASCII', 'x'.repeat(65536)],
    ['multibyte UTF-8', '界'.repeat(21845) + 'x'],
]) {
    test(`publishes ${name} Markdown at the byte limit unchanged`, async t => {
        const result = await runPublisher(t, [success(markdown)]);
        assert.equal(result.status, 0, result.stderr);
        assert.deepEqual(result.calls, [['pr', 'comment', '42', '--repo', 'example/repository', '--body', markdown]]);
        assert.ok(result.summary.includes(markdown));
    });
}

for (const [name, markdown] of [
    ['ASCII', 'x'.repeat(65537)],
    ['multibyte UTF-8', '界'.repeat(21845) + 'xx'],
]) {
    test(`rejects ${name} Markdown one byte over the limit before calling the helper`, async t => {
        const result = await runPublisher(t, [success(markdown)]);
        assert.equal(result.status, 1);
        assert.deepEqual(result.calls, []);
        assert.match(result.stderr, /65537 UTF-8 bytes, exceeding the 65536-byte publication limit/);
        assert.match(result.summary, /No comment was posted/);
        assert.match(result.summary, /full review is available in the workflow summary/);
        assert.ok(result.summary.includes(markdown));
    });
}

test('includes the permission-denial warning in the comment byte budget', async t => {
    const withDenial = markdown => [{
        ...success(markdown),
        permission_denials: [{ tool_name: 'Bash', tool_input: {} }],
    }];
    // Obtain the actual appended warning without duplicating its wording here.
    const probe = await runPublisher(t, withDenial('Review.'));
    assert.equal(probe.status, 0, probe.stderr);
    const warning = probe.calls[0][6].slice('Review.'.length);
    assert.ok(warning.includes('Warning:'));
    const markdown = 'x'.repeat(65536 - Buffer.byteLength(warning, 'utf8'));
    const boundary = await runPublisher(t, withDenial(markdown));
    assert.equal(boundary.status, 0, boundary.stderr);
    assert.equal(boundary.calls.length, 1);
    assert.equal(boundary.calls[0][6], markdown + warning);

    const oversized = await runPublisher(t, withDenial(markdown + 'x'));
    assert.equal(oversized.status, 1);
    assert.deepEqual(oversized.calls, []);
    assert.match(oversized.summary, /65537 UTF-8 bytes/);
    assert.match(oversized.summary, /Tool permission denials: 1/);
    assert.ok(oversized.summary.includes(markdown + 'x'));
});

for (const [name, env] of [
    ['missing repository', { GH_REPO: '' }],
    ['repository without owner', { GH_REPO: 'repository' }],
    ['repository with an extra path', { GH_REPO: 'example/repository/other' }],
    ['repository with whitespace', { GH_REPO: 'example/repository ' }],
    ['repository with a newline', { GH_REPO: 'example/repository\n' }],
    ['missing PR number', { PR_NUMBER: '' }],
    ['zero PR number', { PR_NUMBER: '0' }],
    ['negative PR number', { PR_NUMBER: '-1' }],
    ['non-integer PR number', { PR_NUMBER: '1.5' }],
    ['PR number with a newline', { PR_NUMBER: '42\n' }],
]) {
    test(`rejects ${name} before invoking the helper`, async t => {
        const result = await runPublisher(t, [success('Review completed.')], { env });
        assert.equal(result.status, 1);
        assert.deepEqual(result.calls, []);
        assert.match(result.stderr, /GH_REPO and PR_NUMBER must identify the triggering pull request/);
        assert.match(result.summary, /Publication not confirmed/);
    });
}

for (const [name, markdown] of [
    ['no findings', '## Code review\n\nNo issues found.'],
    ['plugin skip', 'This pull request is a draft, so I skipped the review.'],
]) {
    test(`preserves the plugin's ${name} final response`, async t => {
        const result = await runPublisher(t, [success(markdown)]);
        assert.equal(result.status, 0, result.stderr);
        assert.deepEqual(result.calls, [['pr', 'comment', '42', '--repo', 'example/repository', '--body', markdown]]);
        assert.ok(result.summary.includes(markdown));
    });
}

test('publishes a warning for denials and summarizes grouped tool names without private inputs', async t => {
    const markdown = '## Review\n\nThe boundary check drops the final item.';
    const secret = 'PRIVATE_EXECUTION_SENTINEL_791';
    const result = await runPublisher(t, [
        { type: 'assistant', message: { content: [{ type: 'text', text: secret }] } },
        { type: 'user', message: { content: [{ type: 'tool_result', content: secret }] } },
        {
            ...success(markdown),
            permission_denials: [
                { tool_name: 'Bash', tool_use_id: secret, tool_input: { command: secret } },
                { tool_name: 'Read', tool_use_id: secret, tool_input: { file_path: secret } },
                { tool_name: 'Bash', tool_use_id: secret, tool_input: { command: secret } },
            ],
        },
    ]);
    assert.equal(result.status, 0, result.stderr);
    assert.equal(result.calls.length, 1);
    assert.deepEqual(result.calls[0].slice(0, 6), ['pr', 'comment', '42', '--repo', 'example/repository', '--body']);
    assert.ok(result.calls[0][6].startsWith(markdown + '\n\n'));
    assert.match(result.calls[0][6], /Warning: Claude encountered 3 tool permission denial\(s\)/);
    assert.match(result.calls[0][6], /review may be incomplete/);
    assert.match(result.summary, /Tool permission denials: 3/);
    assert.match(result.summary, /\| Bash \| 2 \|/);
    assert.match(result.summary, /\| Read \| 1 \|/);
    assert.match(result.stdout, /::warning::/);
    for (const output of [result.stdout, result.stderr, result.summary, result.calls[0][6]]) {
        assert.ok(!output.includes(secret));
        assert.ok(!output.includes('tool_input'));
        assert.ok(!output.includes('tool_use_id'));
    }
});

test('replaces unsafe denial names instead of rendering arbitrary diagnostic text', async t => {
    const secret = 'PRIVATE_TOOL_NAME_SENTINEL_812';
    const result = await runPublisher(t, [{
        ...success('Review completed.'),
        permission_denials: [
            { tool_name: `Bash\n${secret}`, tool_input: {} },
            { tool_name: `| ${secret} |`, tool_input: {} },
        ],
    }]);
    assert.equal(result.status, 0, result.stderr);
    assert.match(result.summary, /\| Unknown tool \| 2 \|/);
    assert.ok(![result.stdout, result.stderr, result.summary].join('\n').includes(secret));
});

test('does not publish a valid result when the action failed', async t => {
    const markdown = 'A final response exists despite the action failure.';
    const result = await runPublisher(t, [success(markdown)], {
        env: { CLAUDE_ACTION_OUTCOME: 'failure' },
    });
    assert.equal(result.status, 1);
    assert.deepEqual(result.calls, []);
    assert.match(result.summary, /Action outcome: failure/);
    assert.match(result.summary, /Publication not confirmed/);
    assert.ok(result.summary.includes(markdown));
});

for (const outcome of ['cancelled', 'skipped']) {
    test(`does not publish when the action was ${outcome}`, async t => {
        const result = await runPublisher(t, [success('This must not be posted.')], {
            env: { CLAUDE_ACTION_OUTCOME: outcome },
        });
        assert.equal(result.status, 0, result.stderr);
        assert.deepEqual(result.calls, []);
        assert.ok(result.summary.includes(`Action outcome: ${outcome}`));
        assert.match(result.summary, /Not attempted because analysis was cancelled or skipped/);
        assert.ok(!result.summary.includes('Published:'));
    });
}

test('fails closed for an unknown action outcome', async t => {
    const result = await runPublisher(t, [success('Do not post this response.')], {
        env: { CLAUDE_ACTION_OUTCOME: 'unexpected' },
    });
    assert.equal(result.status, 1);
    assert.deepEqual(result.calls, []);
    assert.match(result.summary, /Action outcome: unknown/);
});

test('reports a missing execution file without attempting publication', async t => {
    const result = await runPublisher(t, [], { missingExecution: true });
    assert.equal(result.status, 1);
    assert.deepEqual(result.calls, []);
    assert.match(result.summary, /execution file is missing or unreadable/);
});

test('does not expose the source of a JSON parse error', async t => {
    const secret = 'PRIVATE_PARSE_ERROR_SENTINEL_482';
    const result = await runPublisher(t, [], { rawExecution: `[{"type":"${secret}", BROKEN]` });
    assert.equal(result.status, 1);
    assert.deepEqual(result.calls, []);
    assert.match(result.summary, /execution file is not valid JSON/);
    assert.ok(![result.stdout, result.stderr, result.summary].join('\n').includes(secret));
});

for (const [name, messages] of [
    ['non-array document', success('Not an array.')],
    ['null document', null],
    ['null message', [null, success('Invalid messages.')]],
    ['primitive message', [17, success('Invalid messages.')]],
    ['message without type', [{ message: {} }, success('Invalid messages.')]],
    ['message with non-string type', [{ type: 17 }, success('Invalid messages.')]],
    ['zero results', [{ type: 'assistant', message: { content: [] } }]],
    ['multiple results', [success('First.'), success('Second.')]],
    ['error subtype', [{ ...success('Not a successful result.'), subtype: 'error_max_turns' }]],
    ['is_error true', [{ ...success('An error disguised as success.'), is_error: true }]],
    ['missing is_error', [{ ...success('No success flag.'), is_error: undefined }]],
    ['missing result', [{ ...success('Removed.'), result: undefined }]],
    ['non-string result', [{ ...success('Replaced.'), result: { text: 'Invalid.' } }]],
    ['whitespace result', [success(' \n\t ')]],
    ['invalid permission_denials', [{ ...success('Invalid denials.'), permission_denials: {} }]],
]) {
    test(`rejects ${name} without publishing`, async t => {
        const result = await runPublisher(t, messages);
        assert.equal(result.status, 1);
        assert.deepEqual(result.calls, []);
        assert.match(result.stderr, /::error::/);
        assert.match(result.summary, /Publication not confirmed/);
        assert.ok(!result.summary.includes('Published:'));
    });
}

test('does not expose result errors in diagnostics', async t => {
    const secret = 'PRIVATE_RESULT_ERROR_SENTINEL_316';
    const result = await runPublisher(t, [{
        type: 'result', subtype: 'error_during_execution', is_error: true,
        errors: [secret], permission_denials: [],
    }]);
    assert.equal(result.status, 1);
    assert.deepEqual(result.calls, []);
    assert.ok(![result.stdout, result.stderr, result.summary].join('\n').includes(secret));
});

test('does not retry or expose fake gh stderr after publication failure', async t => {
    const secret = 'PRIVATE_GH_STDERR_SENTINEL_613';
    const result = await runPublisher(t, [success('A review ready to publish.')], {
        env: { FAKE_GH_EXIT: '1', FAKE_GH_STDERR: secret },
    });
    assert.equal(result.status, 1);
    assert.equal(result.calls.length, 1);
    assert.match(result.summary, /publication helper failed/);
    assert.match(result.summary, /No automatic retry was attempted/);
    assert.ok(![result.stdout, result.stderr, result.summary].join('\n').includes(secret));
    assert.ok(!result.summary.includes('Published:'));
});

for (const [name, output] of [
    ['no URL', ''],
    ['wrong pull request', 'https://github.com/example/repository/pull/43#issuecomment-1234'],
    ['wrong repository', 'https://github.com/example/other/pull/42#issuecomment-1234'],
    ['wrong host', 'https://github.example.com/example/repository/pull/42#issuecomment-1234'],
    ['malformed URL', 'https://github.com/example/repository/pull/42#issuecomment-invalid'],
    ['extra output', commentUrl + '\nAn additional line.'],
]) {
    test(`does not confirm publication when the helper returns ${name}`, async t => {
        const result = await runPublisher(t, [success('Review completed.')], {
            env: { FAKE_GH_OUTPUT: output },
        });
        assert.equal(result.status, 1);
        assert.equal(result.calls.length, 1);
        assert.match(result.summary, /Publication not confirmed/);
        assert.match(result.summary, /No automatic retry was attempted/);
        assert.ok(!result.summary.includes('Published:'));
    });
}

test('preserves multiline quotes and shell substitutions as one literal body argument', async t => {
    let markdown;
    const result = await runPublisher(t, directory => {
        const substitution = join(directory, 'substitution-ran').replaceAll('\\', '/');
        const backtick = join(directory, 'backtick-ran').replaceAll('\\', '/');
        markdown = `## Review\n\nLiteral "double quotes" and 'single quotes'.\n\`touch "${backtick}"\`\n$(touch "${substitution}")\nFinal line.`;
        return [success(markdown)];
    });
    assert.equal(result.status, 0, result.stderr);
    assert.deepEqual(result.calls, [['pr', 'comment', '42', '--repo', 'example/repository', '--body', markdown]]);
    assert.ok(result.summary.includes(markdown));
    await assert.rejects(access(join(result.directory, 'substitution-ran')), { code: 'ENOENT' });
    await assert.rejects(access(join(result.directory, 'backtick-ran')), { code: 'ENOENT' });
});
