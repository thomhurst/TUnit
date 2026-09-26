import { dotnet } from './_framework/dotnet.js';

await dotnet
    .withApplicationArguments('--minimum-expected-tests', '1')
    .withExitCodeLogging()
    .withElementOnExit()
    .run();
