module Naggum.Compiler.StringGen

open IGenerator
open System
open System.Reflection
open System.Reflection.Emit

type StringGen(str : string) =
    interface IGenerator with
        member this.Generate ilGen =
            ilGen.Emit(OpCodes.Ldstr,str)
        member this.ReturnTypes () =
            [typeof<string>]