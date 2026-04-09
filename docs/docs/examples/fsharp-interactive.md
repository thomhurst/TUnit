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
   #r "nuget: TUnit, 1.6.0"
   ```

2. **Write your tests**: You can write your tests in the same way you would in a regular F# project. Because TUnit discovers tests via a source generator at build time, scripts must declare tests dynamically using `[<DynamicTestBuilder>]`. For example:

   ```fsharp
      #r "nuget: TUnit"
      #r "nuget: TUnit.Assertions.FSharp"

      open System
      open TUnit.Core
      open TUnit.Assertions
      open TUnit.Assertions.Extensions
      open TUnit.Assertions.FSharp.Operations
      open TUnit.Engine.Extensions
      open Microsoft.Testing.Platform.Builder

      type Tests() =
         member _.Basic() =
            Console.WriteLine("This is a basic test")

         member _.DataDrivenArguments(a: int, b: int, c: int) =
            task {
                  Console.WriteLine("This one can accept arguments")
                  let result = a + b
                  do! check (Assert.That(result).IsEqualTo(c))
            }

         [<DynamicTestBuilder>]
         member _.BuildTests(context: DynamicTestBuilderContext) =
            context.AddTest(DynamicTest<Tests>(TestMethod = fun instance -> instance.Basic()))

            context.AddTest(
               DynamicTest<Tests>(
                  TestMethod = fun instance ->
                     instance.DataDrivenArguments(1, 2, 3) |> ignore
               )
            )

      // Bootstrap Microsoft.Testing.Platform the same way a compiled
      // TUnit project's Program.cs does, then run the host.
      let runAsync (args: string array) =
         task {
            let! builder = TestApplication.CreateBuilderAsync(args)
            builder.AddTUnit()
            use! app = builder.BuildAsync()
            return! app.RunAsync()
         }

      printfn "Running tests..."
      let exitCode =
         runAsync [| "--results-directory"; System.IO.Directory.GetCurrentDirectory() |]
         |> Async.AwaitTask
         |> Async.RunSynchronously

      printfn "Exit code: %d" exitCode
   ```

   :::caution Experimental
   TUnit is designed around a source generator that discovers tests at build time. In
   F# Interactive there is no build step, so **only tests added through
   `[<DynamicTestBuilder>]` are discovered** — plain `[<Test>]`-annotated members on
   script-defined types will not run. If you need the full TUnit experience (attributes,
   data sources, hooks, filters) prefer a regular `.fsproj` project targeting TUnit
   rather than a `.fsx` script.
   :::

3. **Run your tests**: Execute the script in F# Interactive. Results are printed to the console.

   ```powershell
   dotnet fsi your_script.fsx
   ```

