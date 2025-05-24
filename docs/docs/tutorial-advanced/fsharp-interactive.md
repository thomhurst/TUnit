---
sidebar_position: 31
---

# F# Interactive

F# Interactive (FSI) is a REPL (Read-Eval-Print Loop) for F#. It allows you to execute F# code interactively, making it a powerful tool for testing and prototyping.
It is included with the F# compiler and can be used in various development environments, including Visual Studio and Visual Studio Code.

## Using F# Interactive with TUnit

TUnit can be used with F# Interactive to run tests interactively. This is particularly useful for rapid prototyping, testing small code snippets, and running tests in an easy script
format. To use TUnit with F# Interactive, follow these steps:

1. **Add TUnit to your fsx script**: You need to reference the TUnit library in your F# script. You can do this by adding the following line at the top of your `.fsx` file:
   The following will add the latest version of TUnit to your script:

   ```fsharp
   #r "nuget: TUnit"
   ```

   Alternatively, you can specify a specific version:

   ```fsharp
   #r "nuget: TUnit, 0.20.16"
   ```

2. **Write your tests**: You can write your tests in the same way you would in a regular F# project. For example:

   ```fsharp
      #r "nuget: TUnit, 0.20.16"
      #r "nuget: TUnit.Assertions.Fsharp, 0.20.16"

      open System
      open TUnit
      open TUnit.Engine
      open TUnit.Core
      open TUnit.Assertions
      open TUnit.Assertions
      open TUnit.Assertions.Extensions
      open TUnit.Assertions.FSharp.Operations
      open TUnit.Engine.Services
      open System.Collections.Generic

      type tests() =
         [<Test>]
         member _.Basic() =
            Console.WriteLine("This is a basic test")

         [<Test>]
         [<Arguments(1, 2, 3)>]
         [<Arguments(2, 3, 5)>]
         member _.DataDrivenArguments(a: int, b: int, c: int) =
            async {
                  Console.WriteLine("This one can accept arguments from an attribute")
                  let result = a + b
                  do! check (Assert.That(result).IsEqualTo(c))
            }

         [<DynamicTestBuilder>]
         member _.BuildTests(context: DynamicTestBuilderContext) =
            context.AddTest(DynamicTest<tests>(TestMethod = fun instance -> instance.Basic()))

            context.AddTest(
                  DynamicTest<tests>(
                     TestMethod = fun instance -> instance.DataDrivenArguments(1, 2, 3) |> Async.RunSynchronously
                  )
            )

      // Instantiate your test class
      let testInstance = tests ()

      testInstance.BuildTests

      // Create a test runner and get the results
      let resultsTask =
         task {
            // Set results directory to current working directory
            let args = [| "--results-directory"; System.IO.Directory.GetCurrentDirectory() |]
            let! testResults = TUnitRunner.RunTests(args)
            return testResults
         }

      printf "Running tests..."

      resultsTask |> Async.AwaitTask |> Async.RunSynchronously
   ```

3. **Run your tests**: You can run your tests by executing the script in F# Interactive. The results will be printed to the console.
   To run the script, you can use the following command

   ```powershell
   dotnet fsi your_script.fsx
   ```

