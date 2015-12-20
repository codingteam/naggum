﻿module Naggum.Assembler.Assembler

open System
open System.Reflection
open System.Reflection.Emit

open Naggum.Assembler.Representation
open Naggum.Compiler
open Naggum.Compiler.Reader

let processMetadataItem = function
    | Atom (Symbol ".entrypoint") -> EntryPoint
    | other -> failwithf "Unrecognized metadata item definition: %A" other

let resolveAssembly _ =
    Assembly.GetAssembly(typeof<Int32>) // TODO: Assembly resolver

let resolveType name =
    let result = Type.GetType name // TODO: Resolve types from the assembler context
    if isNull result then
        failwithf "Type %s could not be found" name

    result

let resolveTypes =
    List.map (function 
              | Atom (Symbol name) -> resolveType name
              | other -> failwithf "Unrecognized type: %A" other)

let processMethodSignature = function
    | [Atom (Symbol assembly)
       Atom (Symbol typeName)
       Atom (Symbol methodName)
       List argumentTypes
       Atom (Symbol returnType)] ->
        { Assembly = Some (resolveAssembly assembly) // TODO: Resolve types from current assembly
          ContainingType = Some (resolveType typeName) // TODO: Resolve methods without a type (e.g. assembly methods)
          Name = methodName
          ArgumentTypes = resolveTypes argumentTypes
          ReturnType = resolveType returnType }
    | other -> failwithf "Unrecognized method signature: %A" other

let processInstruction = function
    | List ([Atom (Symbol "ldstr"); Atom (Object (:? string as s))]) ->
        Ldstr s
    | List ([Atom (Symbol "call"); List (calleeSignature)]) ->
        let signature = processMethodSignature calleeSignature
        Call signature
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
    | List (Atom (Symbol ".method")
            :: Atom (Symbol name)
            :: List argumentTypes
            :: Atom (Symbol returnType)
            :: List metadata
            :: body) ->
        let definition =
            { Metadata = Set.empty
              Visibility = Public // TODO: Determine method visibility
              Name = name
              ArgumentTypes = resolveTypes argumentTypes
              ReturnType = resolveType returnType
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
