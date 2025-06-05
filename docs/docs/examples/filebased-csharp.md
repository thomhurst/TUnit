# File based C# application

Starting with dotnet 10 (preview 4), you can use the new file-based C# application feature to run tests in a more straightforward way.

## Using File-Based C# Application with TUnit

To use TUnit with a file-based C# application, you can follow these steps:

1.  **Create a new C# file**: Create a new file with a `.cs` extension, for example, `Program.cs`.
2.  **Add TUnit to your project**: You can add TUnit as a package reference in your file. At the top of your `Program.cs`, add the following line:

    ```csharp
    #:package TUnit@0.*
    ```

    Alternatively, you can specify a specific version:

    ```csharp
    #:package TUnit@0.25.0
    ```

3.  **Write your tests**: You can write your tests in the same way you would in a regular C# project. For example:

        ```csharp
        #:package TUnit@0.*

        using TUnit;
        public class Tests
        {
            [Test]
            public void Basic()
            {
                Console.WriteLine("This is a basic test");
            }

            [Test]
            [Arguments(1, 2, 3)]
            [Arguments(2, 3, 5)]
            public async Task DataDrivenArguments(int a, int b, int c)
            {
                Console.WriteLine("This one can accept arguments from an attribute");
                var result = a + b;
                await Assert.That(result).IsEqualTo(c);
            }

        }

        ```

    4 **Run your tests**: You can run your tests by executing the script in F# Interactive. The results will be printed to the console.
    To run the script, you can use the following command

    ```powershell
    dotnet run Program.cs
    ```

If you need to convert the file based application to a regular C# project, you can run the following command:

    ```powershell
    dotnet project convert Program.cs
    ```
