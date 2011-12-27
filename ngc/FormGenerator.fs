(*  Copyright (C) 2011 by ForNeVeR, Hagane

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
module Naggum.Compiler.FormGenerator

open System.Collections.Generic
open System.Reflection
open System.Reflection.Emit
open Naggum.Reader
open Naggum.Compiler.IGenerator
open Naggum.Compiler.Context

type FormGenerator() =
    interface IGenerator
        with member this.Generate _ = failwith "Internal compiler error: unreified form generator invoked"

type ValueGenerator(context:Context,value:Value) =
    inherit FormGenerator()
    interface IGenerator
        with member this.Generate _ = failwith "Internal compiler error: unreified value generator invoked"

type SymbolGenerator(context:Context,name:string) =
    inherit ValueGenerator(context,Symbol name)
    interface IGenerator
        with member this.Generate ilGen =
                try
                    let local = context.locals.[name]
                    ilGen.Emit(OpCodes.Ldloc,local)
                with
                | :? KeyNotFoundException -> failwithf "Symbol %A not bound." name

type SequenceGenerator(context:Context,typeBuilder:TypeBuilder,seq:SExp list, gf:IGeneratorFactory) =
    member private this.gen_seq (ilGen:ILGenerator,seq:SExp list) =
        match seq with
            | [] -> ilGen.Emit(OpCodes.Ldnull)
            | [last] ->
                let gen = gf.MakeGenerator(last)
                gen.Generate ilGen
            | sexp :: rest ->
                let gen = gf.MakeGenerator(sexp)
                gen.Generate ilGen
                this.gen_seq (ilGen, rest)
    interface IGenerator with
        member this.Generate ilGen = this.gen_seq (ilGen,seq)

type BodyGenerator(context:Context,typeBuilder:TypeBuilder,body:SExp list, gf:IGeneratorFactory) =
    member private this.gen_body (ilGen:ILGenerator,body:SExp list) =
        match body with
            | [] -> ilGen.Emit(OpCodes.Ldnull)
            | [last] ->
                let gen = gf.MakeGenerator(last)
                gen.Generate ilGen
            | sexp :: rest ->
                let gen = gf.MakeGenerator(sexp)
                gen.Generate ilGen
                ilGen.Emit(OpCodes.Pop)
                this.gen_body (ilGen,body)
    interface IGenerator with
        member this.Generate ilGen = this.gen_body (ilGen,body)