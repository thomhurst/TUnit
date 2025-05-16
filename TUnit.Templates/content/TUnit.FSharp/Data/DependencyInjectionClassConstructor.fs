namespace TestProject

open System
open TUnit.Core.Interfaces
open TUnit.Core

type DependencyInjectionClassConstructor() =
    interface IClassConstructor with
        member _.Create(typ: System.Type, classConstructorMetadata: ClassConstructorMetadata) : obj =
            Console.WriteLine("You can also control how your test classes are new'd up, giving you lots of power and the ability to utilise tools such as dependency injection")
            //if typ = typeof<AndEvenMoreTests> then
            //    AndEvenMoreTests(DataClass()) :> obj
            //else
            //    raise (NotImplementedException())