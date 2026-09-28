import { spawn } from 'node:child_process';
import { appendFile, readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

// Only these errors may reach logs or the summary. Parser errors, child output,
// and intermediate SDK messages can contain private input and must stay private.
class ReviewError extends Error {}

function inspectExecution(text) {
    let messages;
    try {
        messages = JSON.parse(text);
    } catch {
        throw new ReviewError('The execution file is not valid JSON.');
    }
    if (!Array.isArray(messages) || messages.some(message =>
        !message || typeof message !== 'object' || typeof message.type !== 'string')) {
        throw new ReviewError('The execution file must contain an array of SDK messages.');
    }
    const results = messages.filter(message => message.type === 'result');
    if (results.length !== 1) {
        throw new ReviewError('The execution file must contain exactly one result message.');
    }
    const result = results[0];
    const denials = result.permission_denials ?? [];
    if (!Array.isArray(denials)) {
        throw new ReviewError('The result has an invalid permission_denials field.');
    }
    const counts = new Map();
    for (const denial of denials) {
        // Tool names are identifiers, not Markdown or arbitrary diagnostic text.
        const name = typeof denial?.tool_name === 'string'
            && /^[A-Za-z0-9_.:-]{1,200}$/.test(denial.tool_name)
            ? denial.tool_name : 'Unknown tool';
        counts.set(name, (counts.get(name) ?? 0) + 1);
    }
    return { result, counts, denialCount: denials.length };
}

function validateTarget(repo, pr) {
    if (!/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(repo ?? '')
        || !/^[1-9][0-9]*$/.test(pr ?? '')) {
        throw new ReviewError('GH_REPO and PR_NUMBER must identify the triggering pull request.');
    }
}

async function publish(body) {
    const helper = fileURLToPath(new URL('./pr-review-comment.sh', import.meta.url));
    const output = await new Promise((resolve, reject) => {
        // Pass Markdown as one literal argument, never shell source or a file path.
        const child = spawn('bash', [helper, body], {
            shell: false,
            stdio: ['ignore', 'pipe', 'ignore'],
        });
        let stdout = '';
        let tooLong = false;
        child.stdout.setEncoding('utf8');
        child.stdout.on('data', chunk => {
            if (stdout.length + chunk.length > 4096) {
                tooLong = true;
            } else if (!tooLong) {
                stdout += chunk;
            }
        });
        child.on('error', () => reject(new ReviewError('The publication helper could not be started.')));
        child.on('close', code => {
            if (code !== 0) {
                reject(new ReviewError('The publication helper failed. Check its access to the triggering pull request.'));
            } else if (tooLong) {
                reject(new ReviewError('The publication helper did not return a valid comment URL.'));
            } else {
                resolve(stdout.trim());
            }
        });
    });
    // gh pr comment prints the created issue-comment URL. Reject other PRs,
    // hosts, paths and extra output instead of claiming publication succeeded.
    const expected = `https://github.com/${process.env.GH_REPO}/pull/${process.env.PR_NUMBER}#issuecomment-`;
    if (!output.startsWith(expected) || !/^[1-9][0-9]*$/.test(output.slice(expected.length))) {
        throw new ReviewError('The publication helper did not return a comment URL for the triggering pull request.');
    }
    return output;
}

async function main() {
    const outcome = process.env.CLAUDE_ACTION_OUTCOME;
    const actionStatus = ['success', 'failure', 'cancelled', 'skipped'].includes(outcome)
        ? outcome : 'unknown';
    let resultStatus = 'Not inspected.';
    let execution;
    let review;
    let publication = 'Not attempted.';
    let failed = false;

    try {
        if (actionStatus === 'cancelled' || actionStatus === 'skipped') {
            publication = 'Not attempted because analysis was cancelled or skipped.';
        } else {
            let text;
            try {
                text = await readFile(process.env.CLAUDE_EXECUTION_FILE, 'utf8');
            } catch {
                throw new ReviewError('The execution file is missing or unreadable.');
            }
            execution = inspectExecution(text);
            const { result, denialCount } = execution;
            if (result.subtype !== 'success' || result.is_error !== false) {
                throw new ReviewError('Claude did not return a successful result.');
            }
            if (typeof result.result !== 'string' || !result.result.trim()) {
                throw new ReviewError('Claude did not return nonempty final Markdown.');
            }
            review = result.result;
            resultStatus = 'Valid final Markdown received.';
            if (actionStatus !== 'success') {
                throw new ReviewError('The Claude action did not succeed; its result will not be published.');
            }
            validateTarget(process.env.GH_REPO, process.env.PR_NUMBER);
            if (!process.env.GITHUB_STEP_SUMMARY) {
                throw new ReviewError('GITHUB_STEP_SUMMARY is unavailable.');
            }
            const warning = denialCount > 0
                ? `\n\n---\n\n> Warning: Claude encountered ${denialCount} tool permission denial(s). This review may be incomplete. See the workflow summary for tool names and counts; reasons for individual denials are not available in the execution result.`
                : '';
            const url = await publish(review + warning);
            publication = `Published: ${url}`;
            console.log(publication);
            if (denialCount > 0) {
                console.log(`::warning::Claude encountered ${denialCount} tool permission denial(s); the published review may be incomplete.`);
            }
        }
    } catch (error) {
        const reason = error instanceof ReviewError ? error.message : 'An unexpected review processing error occurred.';
        if (!review) resultStatus = reason;
        publication = `Publication not confirmed: ${reason} No automatic retry was attempted.`;
        console.error(`::error::${reason}`);
        failed = true;
    }

    const summary = [
        '## Claude review',
        '',
        `- Action outcome: ${actionStatus}`,
        `- Result validation: ${resultStatus}`,
        `- Publication: ${publication}`,
        `- Tool permission denials: ${execution ? execution.denialCount : 'unavailable'}`,
    ];
    if (execution?.denialCount) {
        summary.push('', '### Tool permission denials', '', '| Tool | Count |', '| --- | ---: |');
        for (const [name, count] of [...execution.counts].sort(([a], [b]) => a.localeCompare(b))) {
            summary.push(`| ${name} | ${count} |`);
        }
        summary.push('', 'Individual denial reasons are not included in the SDK result.');
    }
    if (review) summary.push('', '### Final review', '', review);
    try {
        await appendFile(process.env.GITHUB_STEP_SUMMARY, summary.join('\n') + '\n');
    } catch {
        console.error('::error::The Claude review summary could not be written.');
        failed = true;
    }
    process.exitCode = failed ? 1 : 0;
}

await main();
