module Naggum.Compiler.MathGenerator

open System.Reflection.Emit

open Naggum.Backend.Reader
open Naggum.Compiler.Context
open Naggum.Compiler.IGenerator

//TODO: Make this useful; i.e. determine eldest type in a numeric tower and convert all junior types to eldest
let tower = dict [(typeof<int32>, 1); (typeof<int64>, 2); (typeof<single>, 3); (typeof<float>, 4); (typeof<obj>,5)]

let maxType types =
    try
        let maxOrder = List.maxBy (fun (t) -> tower.[t]) types
        (Seq.find (fun (KeyValue(o,_)) -> o = maxOrder) tower).Key
    with
    | :? System.Collections.Generic.KeyNotFoundException -> failwithf "Some types of %A are not suitable in an arithmetic expression." types

type ArithmeticGenerator(context:Context,typeBuilder:TypeBuilder,args:SExp list, operation:OpCode, gf:IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            //making this just for the sake of return types
            let max_type = (gf.MakeSequence context args).ReturnTypes() |> maxType
            //loading first arg manually so it won't be succeeded by operation opcode
            let arg_gen = gf.MakeGenerator context (List.head args)
            let arg_type = arg_gen.ReturnTypes() |> List.head
            arg_gen.Generate ilGen
            if not (arg_type = max_type) then
                ilGen.Emit(OpCodes.Newobj, max_type.GetConstructor [|arg_type|])
            for arg in List.tail args do
                let arg_gen = gf.MakeGenerator context arg
                let arg_type = arg_gen.ReturnTypes() |> List.head
                arg_gen.Generate ilGen
                ilGen.Emit(operation)
        member this.ReturnTypes () =
            [List.map (fun (sexp) -> (gf.MakeGenerator context sexp).ReturnTypes() |> List.head) args |> maxType]

type SimpleLogicGenerator(context:Context,typeBuilder:TypeBuilder,arg_a:SExp, arg_b:SExp, operation:OpCode, gf:IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            let a_gen = gf.MakeGenerator context arg_a
            let b_gen = gf.MakeGenerator context arg_b
            a_gen.Generate ilGen |> ignore
            b_gen.Generate ilGen |> ignore
            ilGen.Emit(operation)
        member this.ReturnTypes () =
            [typeof<bool>]
