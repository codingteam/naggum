module Generator

open System.Reflection
open System.Reflection.Emit

type IGenerator =
    interface
        abstract Generate : ILGenerator -> unit
    end

    