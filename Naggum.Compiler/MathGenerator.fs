module Naggum.Compiler.MathGenerator

open Naggum.Backend.Reader
open Naggum.Compiler.Context
open Naggum.Compiler.IGenerator

type ArithmeticOperation =
    | Add
    | Div
    | Mul
    | Sub

type LogicOperation =
    | Ceq
    | Cgt
    | Clt

//TODO: Make this useful; i.e. determine eldest type in a numeric tower and convert all junior types to eldest
let tower = dict [(typeof<int32>, 1); (typeof<int64>, 2); (typeof<single>, 3); (typeof<float>, 4); (typeof<obj>,5)]

let maxType types =
    try
        let maxOrder = List.maxBy (fun (t) -> tower.[t]) types
        (Seq.find (fun (KeyValue(o,_)) -> o = maxOrder) tower).Key
    with
    | :? System.Collections.Generic.KeyNotFoundException -> failwithf "Some types of %A are not suitable in an arithmetic expression." types

type ArithmeticGenerator (context : Context,
                          args : SExp list,
                          operation : ArithmeticOperation,
                          gf : IGeneratorFactory) =
    interface IGenerator with
        member __.Generate il =
            // making this just for the sake of return types
            let maxType = (gf.MakeSequence context args).ReturnTypes() |> maxType
            // loading first arg manually so it won't be succeeded by operation opcode
            let argGen = gf.MakeGenerator context (List.head args)
            let argType = argGen.ReturnTypes() |> List.head
            argGen.Generate il
            if argType <> maxType then
                il.Newobj <| maxType.GetConstructor [|argType|]
            for arg in List.tail args do
                let argGen = gf.MakeGenerator context arg
                let argType = argGen.ReturnTypes() |> List.head
                argGen.Generate il
                match operation with
                | Add -> il.Add ()
                | Div -> il.Div false
                | Mul -> il.Mul ()
                | Sub -> il.Sub ()

        member this.ReturnTypes () =
            [List.map (fun (sexp) -> (gf.MakeGenerator context sexp).ReturnTypes() |> List.head) args |> maxType]

type SimpleLogicGenerator (context : Context,
                           argA : SExp,
                           argB : SExp,
                           operation : LogicOperation,
                           gf : IGeneratorFactory) =
    interface IGenerator with
        member __.Generate il =
            let aGen = gf.MakeGenerator context argA
            let bGen = gf.MakeGenerator context argB
            aGen.Generate il |> ignore
            bGen.Generate il |> ignore
            match operation with
            | Ceq -> il.Ceq ()
            | Cgt -> il.Cgt false
            | Clt -> il.Clt false

        member this.ReturnTypes () =
            [typeof<bool>]
