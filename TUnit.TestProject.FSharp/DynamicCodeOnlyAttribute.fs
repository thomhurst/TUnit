namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of DynamicCodeOnlyAttribute.cs

type DynamicCodeOnlyAttribute() =
    inherit System.Attribute()
