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
module Naggum.Compiler.FormGenerator

open System
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
                    let ctxval = context.locals.[name]
                    match ctxval with
                    |Local local ->
                        ilGen.Emit(OpCodes.Ldloc,local)
                    |Arg index ->
                        ilGen.Emit(OpCodes.Ldarg,(int16 index))
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
                this.gen_body (ilGen,rest)
    interface IGenerator with
        member this.Generate ilGen = this.gen_body (ilGen,body)

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
                        scope_subctx.locals.[name] <- Local local
                        let generator = gf.MakeGenerator scope_subctx form
                        generator.Generate ilGen
                        ilGen.Emit (OpCodes.Stloc,local)
                    | other -> failwithf "In let bindings: Expected: (name (form))\nGot: %A\n" other
            | other -> failwithf "In let form: expected: list of bindings\nGot: %A" other
            let bodyGen = (new BodyGenerator (scope_subctx,typeBuilder,body,gf) :> IGenerator)
            bodyGen.Generate ilGen
            ilGen.EndScope()

type ReducedIfGenerator(context:Context,typeBuilder:TypeBuilder,condition:SExp,if_true:SExp,gf:IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            let cond_gen = gf.MakeGenerator context condition
            let if_true_gen = gf.MakeGenerator context if_true
            let if_true_lbl = ilGen.DefineLabel()
            let end_form = ilGen.DefineLabel()
            cond_gen.Generate ilGen
            ilGen.Emit (OpCodes.Brtrue, if_true_lbl)
            ilGen.Emit OpCodes.Ldnull
            ilGen.Emit (OpCodes.Br,end_form)
            ilGen.MarkLabel if_true_lbl
            if_true_gen.Generate ilGen
            ilGen.MarkLabel end_form

type FullIfGenerator(context:Context,typeBuilder:TypeBuilder,condition:SExp,if_true:SExp,if_false:SExp,gf:IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            let cond_gen = gf.MakeGenerator context condition
            let if_true_gen = gf.MakeGenerator context if_true
            let if_false_gen = gf.MakeGenerator context if_false
            let if_true_lbl = ilGen.DefineLabel()
            let end_form = ilGen.DefineLabel()
            cond_gen.Generate ilGen
            ilGen.Emit (OpCodes.Brtrue, if_true_lbl)
            if_false_gen.Generate ilGen
            ilGen.Emit (OpCodes.Br,end_form)
            ilGen.MarkLabel if_true_lbl
            if_true_gen.Generate ilGen
            ilGen.MarkLabel end_form

type FunCallGenerator(context:Context,typeBuilder:TypeBuilder,fname:string,arguments:SExp list,gf:IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            let func = context.functions.[fname]
            let args_seq = gf.MakeSequence context arguments
            args_seq.Generate ilGen
            ilGen.Emit(OpCodes.Call,func)

type DefunGenerator(context:Context,typeBuilder:TypeBuilder,fname:string,parameters:SExp list,body:SExp list,gf:IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            let argsDef = Array.create (List.length parameters) typeof<obj>
            let methodGen = typeBuilder.DefineMethod(fname, MethodAttributes.Public ||| MethodAttributes.Static, typeof<obj>, argsDef)
            let methodILGen = (methodGen.GetILGenerator())
            context.functions.[fname] <- methodGen
            let fun_ctx = new Context(context)
            for parm in parameters do
                match parm with
                | Atom(Symbol parm_name) -> fun_ctx.locals.[parm_name] <- Arg (List.findIndex (fun (p) -> p = parm) parameters)
                | other -> failwithf "In function %A parameter definition:\nExpected: Atom(Symbol)\nGot: %A" fname parm
            let bodyGen = gf.MakeBody fun_ctx body
            bodyGen.Generate methodILGen
            methodILGen.Emit(OpCodes.Ret)

type ClrCallGenerator(context : Context, typeBuilder : TypeBuilder, className : string, methodName : string, arguments : SExp list,
                      gf : IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            let clrType = Type.GetType className
            let argTypes = arguments
                           |> List.map (fun sexp -> match sexp with
                                                    | Atom (Object arg) -> arg.GetType()
                                                    | any               -> failwithf "Cannot use %A in CLR call." any)
                           |> List.toArray
            let clrMethod = clrType.GetMethod(methodName, argTypes)
            ilGen.Emit(OpCodes.Ldnull)
            let args_seq = gf.MakeSequence context arguments
            args_seq.Generate ilGen            
            ilGen.EmitCall(OpCodes.Call, clrMethod, [| |])
