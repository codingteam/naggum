module Naggum.Assembler.Assembler

open System.Reflection.Emit

open Naggum.Compiler

/// Prepares the source file for assembling. Returns the intermediate
/// representation of the source code.
let prepare fileName stream =
    let forms = Reader.parse fileName stream
    ()

/// Assembles the source code. Returns a list of assemblies ready for saving.
let assemble repr : AssemblyBuilder seq =
    Seq.empty
