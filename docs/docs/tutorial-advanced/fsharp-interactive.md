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
   #r "nuget: TUnit, 0.20.11"
   ```

2. **Write your tests**: You can write your tests in the same way you would in a regular F# project. For example:
   ```fsharp

   ```
