module Naggum.Assembler.Assembler

open System
open System.Reflection.Emit

open Naggum.Assembler.Representation
open Naggum.Compiler
open Naggum.Compiler.Reader

let processMetadataItem = function
    | Atom (Symbol ".entrypoint") -> EntryPoint
    | other -> failwithf "Unrecognized metadata item definition: %A" other

let processInstruction = function
    | List ([Atom (Symbol "ldstr"); Atom (Symbol string)]) -> Ldstr string
    | List ([Atom (Symbol "call"); Atom (Symbol methodName)]) -> failwithf "Method calls are not supported now"
    | List ([Atom (Symbol "ret")]) -> Ret
    | other -> failwithf "Unrecognized instruction: %A" other

let addMetadata metadata method' =
    List.fold (fun ``method`` metadataExpr ->
                   let metadataItem = processMetadataItem metadataExpr
                   { ``method`` with Metadata = Set.add metadataItem ``method``.Metadata })
              method'
              metadata

let addBody body method' =
    List.fold (fun ``method`` bodyClause ->
                   let instruction = processInstruction bodyClause
                   { ``method`` with Body = List.append ``method``.Body [instruction] })
              method'
              body

let processAssemblyUnit = function
    | List (Atom (Symbol ".method") :: Atom (Symbol name) :: List arguments :: List metadata :: body) ->
        let definition =
            { Metadata = Set.empty
              Visibility = Public // TODO: Determine method visibility
              Name = name
              ReturnType = typeof<Void> // TODO: Determine method return type
              Body = List.empty }
        definition
        |> addMetadata metadata
        |> addBody body
        |> Method
    | other -> failwithf "Unrecognized assembly unit definition: %A" other

let prepareTopLevel = function
    | List (Atom (Symbol ".assembly") :: Atom (Symbol name) :: units) ->
        { Name = name
          Units = List.map processAssemblyUnit units }
    | other -> failwithf "Unknown top-level construct: %A" other

/// Prepares the source file for assembling. Returns the intermediate
/// representation of the source code.
let prepare fileName stream : Assembly seq =
    let forms = Reader.parse fileName stream
    forms |> Seq.map prepareTopLevel

/// Assembles the source code. Returns a list of assemblies ready for saving.
let assemble repr : AssemblyBuilder seq =
    Seq.empty
