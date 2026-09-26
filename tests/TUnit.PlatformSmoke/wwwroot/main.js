import { dotnet } from './_framework/dotnet.js';

let exitCode = 1;
try {
    exitCode = await dotnet
        .withApplicationArguments('--minimum-expected-tests', '1')
        .run();
} catch (error) {
    console.error(error);
}

// XHarness reads this marker to verify the application's actual exit code.
const completion = document.createElement('label');
completion.id = 'tests_done';
completion.textContent = String(exitCode);
document.body.appendChild(completion);
console.log(`WASM EXIT ${exitCode}`);
