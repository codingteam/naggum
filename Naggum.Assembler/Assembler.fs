module Naggum.Assembler.Assembler

open System
open System.Reflection
open System.Reflection.Emit

open Naggum.Assembler.Representation

let private getMethodAttributes (m : MethodDefinition) =
    let empty = enum 0
    let conditions =
        [ (m.Visibility = Public, MethodAttributes.Public)
          (true, MethodAttributes.Static) ] // TODO: Proper static method detection

    conditions
    |> List.map (fun (c, r) -> if c then r else empty)
    |> List.fold (|||) empty

let private findMethod (signature : MethodSignature) =
    let ``type`` = signature.ContainingType.Value
    ``type``.GetMethod (signature.Name, Array.ofList signature.ArgumentTypes)

let private buildMethodBody (m : MethodDefinition) (builder : MethodBuilder) =
    let generator = builder.GetILGenerator ()

    m.Body
    |> List.iter (function
                  | Call signature ->
                      let methodInfo = findMethod signature
                      generator.Emit (OpCodes.Call, methodInfo)
                  | LdcI4 i -> generator.Emit (OpCodes.Ldc_I4, i)
                  | Ldstr string -> generator.Emit (OpCodes.Ldstr, string)
                  | SimpleInstruction r -> generator.Emit r)

let private assembleUnit (assemblyBuilder : AssemblyBuilder) (builder : ModuleBuilder) = function
    | Method m ->
        let name = m.Name
        let attributes = getMethodAttributes m
        let returnType = m.ReturnType
        let argumentTypes = Array.ofList m.ArgumentTypes
        let methodBuilder = builder.DefineGlobalMethod (name,
                                                        attributes,
                                                        returnType,
                                                        argumentTypes)
        if Set.contains EntryPoint m.Metadata then
            assemblyBuilder.SetEntryPoint methodBuilder
        buildMethodBody m methodBuilder

/// Assembles the intermediate program representation. Returns an assembled
/// module.
let assemble (mode : AssemblyBuilderAccess) (assembly : Assembly) =
    let name = AssemblyName assembly.Name
    let domain = AppDomain.CurrentDomain
    let builder = domain.DefineDynamicAssembly (name, mode)
    let fileName = assembly.Name + ".exe" // TODO: Proper file naming
    let moduleBuilder = builder.DefineDynamicModule (assembly.Name, fileName)
    assembly.Units |> List.iter (assembleUnit builder moduleBuilder)
    moduleBuilder.CreateGlobalFunctions ()
    builder

/// Assembles the intermediate program representation. Returns a list of
/// assemblies ready for saving.
let assembleAll (mode : AssemblyBuilderAccess)
                (assemblies : Assembly seq) : AssemblyBuilder seq =
    assemblies
    |> Seq.map (assemble mode)
