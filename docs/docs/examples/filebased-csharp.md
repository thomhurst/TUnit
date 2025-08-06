# File based C# application

Starting with dotnet 10 (preview 4), you can use the new file-based C# application feature to run tests in a more straightforward way.

## Using File-Based C# Application with TUnit

To use TUnit with a file-based C# application, you can follow these steps:

1.  **Create a new C# file**: Create a new file with a `.cs` extension, for example, `Program.cs`.
2.  **Add TUnit to your project**: You can add TUnit as a package reference in your file. At the top of your `Program.cs`, add the following line:

    ```csharp
    #:package TUnit@0.*
    ```

    -   Alternatively, you can specify a specific version:

        ```csharp
        #:package TUnit@0.25.0
        ```

    -   You can also use msbuild props files to include TUnit. By creating a `Directory.build.props` file in the same directory as the csharp file.

        ```xml
        <Project>
            <ItemGroup>
                <PackageReference Include="TUnit" Version="*" />
            </ItemGroup>
        </Project>
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

4.  **Run your tests**: You can run your tests by executing the script using `dotnet run`. The results will be printed to the console.
    To run the script, you can use the following command

    ```powershell
    dotnet run Program.cs
    ```

If you need to convert the file based application to a regular C# project, you can run the following command:

    ```powershell
    dotnet project convert Program.cs
    ```

## Using msbuild props with File-Based C# Application

Single file csharp applications can also be used with msbuild props files. You can create a `*.props` file and the dotnet sdk will automatically include it when running the file-based application.

1. Create a file named `Directory.build.props` with the following content:

    ```xml
    <Project>
        <ItemGroup>
        <PackageReference Include="TUnit" Version="*" />
        </ItemGroup>
    </Project>
    ```

2. Create a csharp file with the following content:

    ```csharp
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

3. Run the script using the following command:

    ```powershell
    dotnet run file.cs
    ```

This will automatically include the `Directory.build.props` file as long as it is in the same directory as the csharp file, and you will be able to run your tests with TUnit.
