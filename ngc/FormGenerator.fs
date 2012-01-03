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
                let gen = gf.MakeGenerator context last
                gen.Generate ilGen
            | sexp :: rest ->
                let gen = gf.MakeGenerator context sexp
                gen.Generate ilGen
                this.gen_seq (ilGen, rest)
    interface IGenerator with
        member this.Generate ilGen = this.gen_seq (ilGen,seq)

type BodyGenerator(context:Context,typeBuilder:TypeBuilder,body:SExp list, gf:IGeneratorFactory) =
    member private this.gen_body (ilGen:ILGenerator,body:SExp list) =
        match body with
            | [] -> ilGen.Emit(OpCodes.Ldnull)
            | [last] ->
                let gen = gf.MakeGenerator context last
                gen.Generate ilGen
            | sexp :: rest ->
                let gen = gf.MakeGenerator context sexp
                gen.Generate ilGen
                ilGen.Emit(OpCodes.Pop)
                this.gen_body (ilGen,body)
    interface IGenerator with
        member this.Generate ilGen = this.gen_body (ilGen,body)

type DefunGenerator(context:Context,typeBuilder:TypeBuilder,name:string,args:string list,body:SExp list,gf:IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            let argsDef = Array.create (List.length args) typeof<obj>
            let methodGen = typeBuilder.DefineMethod(name, MethodAttributes.Public ||| MethodAttributes.Static, typeof<obj>, argsDef)
            context.functions.[name] <- methodGen //should be before method body generation!
            let methodILGen = (methodGen.GetILGenerator())
            let bodyGen = gf.MakeBody context body
            bodyGen.Generate methodILGen

type LetGenerator(context:Context,typeBuilder:TypeBuilder,bindings:SExp,body:SExp list,gf:IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            ilGen.BeginScope()
            let scope_subctx = new Context (context)
            match bindings with
            | List list ->
                for binding in list do
                    match binding with
                    | List [(Atom (Symbol name)); form] ->
                        let local = ilGen.DeclareLocal(typeof<SExp>)
                        scope_subctx.locals.[name] <- local
                        let generator = gf.MakeGenerator scope_subctx form
                        generator.Generate ilGen
                        ilGen.Emit (OpCodes.Stloc,local)
                    | other -> failwithf "In let bindings: Expected: (name (form))\nGot: %A\n" other
            | other -> failwithf "In let form: expected: list of bindings\nGot: %A" other
            let bodyGen = (new BodyGenerator (scope_subctx,typeBuilder,body,gf) :> IGenerator)
            bodyGen.Generate ilGen
            ilGen.EndScope()