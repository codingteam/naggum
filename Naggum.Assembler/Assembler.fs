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
    // TODO: Add method overload resolution etc. (see ClrGenerator module)
    let ``type`` = signature.ContainingType.Value
    ``type``.GetMethod (signature.Name, Array.ofList signature.ArgumentTypes)

let private buildMethodBody (m : MethodDefinition) (builder : MethodBuilder) =
    let generator = builder.GetILGenerator ()
    
    m.Body
    |> List.iter (function
                  | Call signature ->
                      let methodInfo = findMethod signature
                      generator.Emit (OpCodes.Call, methodInfo)
                  | Ldstr string -> generator.Emit (OpCodes.Ldstr, string)
                  | Ret -> generator.Emit (OpCodes.Ret))

let private assembleUnit (builder : ModuleBuilder) = function
    | Method m ->
        let name = m.Name
        let attributes = getMethodAttributes m
        let returnType = m.ReturnType
        let argumentTypes = Array.ofList m.ArgumentTypes
        let methodBuilder = builder.DefineGlobalMethod (name,
                                                        attributes,
                                                        returnType,
                                                        argumentTypes)
        buildMethodBody m methodBuilder
        

let private assembleAssembly (assembly : Assembly) =
    let name = AssemblyName assembly.Name
    let domain = AppDomain.CurrentDomain
    let builder = domain.DefineDynamicAssembly (name,
                                                AssemblyBuilderAccess.Save)
    let fileName = assembly.Name + ".dll" // TODO: Proper file naming
    let moduleBuilder = builder.DefineDynamicModule (assembly.Name, fileName)
    assembly.Units |> List.iter (assembleUnit moduleBuilder)
    builder

/// Assembles the intermediate program representation. Returns a list of
/// assemblies ready for saving.
let assemble (assemblies : Assembly seq) : AssemblyBuilder seq =
    assemblies
    |> Seq.map assembleAssembly
