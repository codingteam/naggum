(*  Copyright (C) 2011-2012 by ForNeVeR, Hagane

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. *)
module Naggum.Compiler.MathGenerator

open System
open System.Reflection.Emit

open Naggum.Compiler.Reader
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
            let mutable max_type = typeof<int32>
            //loading first arg manually so it won't be succeeded by operation opcode
            let arg_gen = gf.MakeGenerator context (List.head args)
            let arg_types = arg_gen.Generate ilGen
            for arg in List.tail args do
                let arg_gen = gf.MakeGenerator context arg
                let arg_types = arg_gen.Generate ilGen
                if tower.[maxType arg_types] > tower.[max_type] then
                    max_type <- maxType arg_types
                ilGen.Emit(operation)
            ilGen.Emit(OpCodes.Box,max_type)
            [max_type]

type SimpleLogicGenerator(context:Context,typeBuilder:TypeBuilder,arg_a:SExp, arg_b:SExp, operation:OpCode, gf:IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            let a_gen = gf.MakeGenerator context arg_a
            let b_gen = gf.MakeGenerator context arg_b
            a_gen.Generate ilGen |> ignore
            b_gen.Generate ilGen |> ignore
            ilGen.Emit(operation)
            [typeof<bool>]