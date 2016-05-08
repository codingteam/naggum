module Naggum.Compiler.StringGen

open IGenerator

type StringGen (str : string) =
    interface IGenerator with
        member __.Generate il =
            il.Ldstr str
        member this.ReturnTypes () =
            [typeof<string>]
