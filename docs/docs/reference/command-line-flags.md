# Command Line Flags

:::info

Please note that for the coverage and trx report, you need to install [additional extensions](../extensions/extensions.md)

:::

<pre>
    --diagnostic
        Enable the diagnostic logging. The default log level is 'Trace'.
        The file will be written in the output directory with the name log_[MMddHHssfff].diag

    --diagnostic-filelogger-synchronouswrite
        Force the built-in file logger to write the log synchronously.
        Useful for scenario where you don't want to lose any log (i.e. in case of crash).
        Note that this is slowing down the test execution.

    --diagnostic-output-directory
        Output directory of the diagnostic logging.
        If not specified the file will be generated inside the default 'TestResults' directory.

    --diagnostic-file-prefix
        Prefix for the log file name that will replace '[log]_.'

    --diagnostic-verbosity
        Define the level of the verbosity for the --diagnostic.
        The available values are 'Trace', 'Debug', 'Information', 'Warning', 'Error', and 'Critical'.

    --exit-on-process-exit
        Exit the test process if dependent process exits. PID must be provided.

    --help
        Show the command line help.

    --ignore-exit-code
        Do not report non successful exit value for specific exit codes
        (e.g. '--ignore-exit-code 8;9' ignore exit code 8 and 9 and will return 0 in these case)

    --info
        Display .NET test application information.

    --list-tests
        List available tests.

    --log-level
        Controls framework logging and diagnostic features.
        The available values are:
        - 'Trace' or 'Debug': Enable detailed stack traces, discovery diagnostics, timing details
        - 'Information' (default): Normal framework behavior
        - 'Warning', 'Error', 'Critical', or 'None': Minimal framework output

    --minimum-expected-tests
        Specifies the minimum number of tests that are expected to run.

    --results-directory
        The directory where the test results are going to be placed.
        If the specified directory doesn't exist, it's created.
        The default is TestResults in the directory that contains the test application.

    --timeout
        A global test execution timeout.
        Takes one argument as string in the format [h|m|s] where 'value' is float.

    --coverage
        Collect the code coverage using dotnet-coverage tool

    --coverage-output
        Output file

    --coverage-output-format
        Output file format. Supported values: 'coverage', 'xml' and 'cobertura'

    --coverage-settings
        XML code coverage settings

    --disable-logo
        Disables the TUnit logo when starting a test session.
        Can also be set via TUNIT_DISABLE_LOGO environment variable.

    --fail-fast
        Cancel the test run after the first test failure

    --maximum-parallel-tests
        Maximum Parallel Tests

    --no-ansi
        Disable outputting ANSI escape characters to screen.

    --no-progress
        Disable reporting progress to screen.

    --output
        Controls test result detail level AND real-time console output buffering.
        - 'Normal': Show failures only + buffer test output (no real-time spam)
        - 'Detailed': Show all tests + real-time test output

        Smart defaults: 'Normal' for console environments, 'Detailed' for IDE environments.
        This prevents console spam while keeping IDEs responsive (each test has its own output window).

    --reflection
        Enable reflection mode for test discovery and execution (defaults to source generation mode)

    --report-trx
        Enable generating TRX report

    --report-trx-filename
        The name of the generated TRX report

    --test-parameter
        Custom parameters to pass to TUnit

    --treenode-filter
        Use a tree filter to filter down the tests to execute

    --github-reporter-style
        Style for GitHub Actions test reporter output.
        Valid values are 'collapsible' (default) or 'full'.
        'collapsible' wraps detailed test results in expandable HTML blocks.
        'full' displays all test details directly.
</pre>
