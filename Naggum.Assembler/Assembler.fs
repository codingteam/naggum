module Naggum.Assembler.Assembler

open System.Reflection.Emit

open Naggum.Assembler.Representation

/// Assembles the intermediate program representation. Returns a list of assemblies ready for saving.
let assemble (assemblies : Assembly seq) : AssemblyBuilder seq =
    Seq.empty
