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
open Naggum.Runtime
open Naggum.Compiler.Reader
open Naggum.Compiler.Context
open Naggum.Compiler.IGenerator
open Naggum.MaybeMonad
open Naggum.Compiler.Reader

type FormGenerator() =
    interface IGenerator with
        member this.Generate _ = failwith "Internal compiler error: unreified form generator invoked"
        member this.ReturnTypes () = failwithf "Internal compiler error: inferring return type of unreified form"

type ValueGenerator(context:Context,value:Value) =
    inherit FormGenerator()
    interface IGenerator with
        member this.Generate _ = failwith "Internal compiler error: unreified value generator invoked"
        member this.ReturnTypes () = failwithf "Internal compiler error: inferring return type of unreified value"

type SymbolGenerator(context:Context,name:string) =
    inherit ValueGenerator(context,Symbol name)
    interface IGenerator with
        member this.Generate ilGen =
            try
                let ctxval = context.locals.[name]
                match ctxval with
                |Local (local, _) ->
                    ilGen.Emit(OpCodes.Ldloc,local)
                |Arg (index,_) ->
                    ilGen.Emit(OpCodes.Ldarg,(int16 index))
            with
            | :? KeyNotFoundException -> failwithf "Symbol %A not bound." name
        member this.ReturnTypes () =
            match context.locals.[name] with
            |Local (_,t) -> [t]
            |Arg (_,t) -> [t]
            

type SequenceGenerator(context:Context,typeBuilder:TypeBuilder,seq:SExp list, gf:IGeneratorFactory) =
    member private this.gen_seq (ilGen:ILGenerator,seq:SExp list) =
        match seq with
            | [] -> 
                ()
            | [last] ->
                let gen = gf.MakeGenerator context last
                gen.Generate ilGen
            | sexp :: rest ->
                let gen = gf.MakeGenerator context sexp
                ignore (gen.Generate ilGen)
                this.gen_seq (ilGen, rest)
    interface IGenerator with
        member this.Generate ilGen = this.gen_seq (ilGen,seq)
        member this.ReturnTypes () =
            List.map (fun (sexp) -> List.head ((gf.MakeGenerator context sexp).ReturnTypes())) seq

type BodyGenerator(context:Context,typeBuilder:TypeBuilder,body:SExp list, gf:IGeneratorFactory) =
    member private this.gen_body (ilGen:ILGenerator,body:SExp list) =
        match body with
            | [] ->
                ilGen.Emit(OpCodes.Ldnull)
            | [last] ->
                let gen = gf.MakeGenerator context last
                let val_type = gen.ReturnTypes()
                gen.Generate ilGen
                if (val_type = [typeof<System.Void>]) then
                    ilGen.Emit(OpCodes.Ldnull)
            | sexp :: rest ->
                let gen = gf.MakeGenerator context sexp
                let val_type = gen.ReturnTypes()
                gen.Generate ilGen
                if not (List.head val_type = typeof<System.Void>) then
                    ilGen.Emit(OpCodes.Pop)
                this.gen_body (ilGen,rest)
    interface IGenerator with
        member this.Generate ilGen = 
            this.gen_body (ilGen,body)
        member this.ReturnTypes () =
            match body with
            |[] -> [typeof<System.Void>]
            |somelist -> 
                let tail_type = (gf.MakeGenerator context (List.rev body |> List.head)).ReturnTypes()
                if tail_type = [typeof<System.Void>] then
                    [typeof<obj>]
                else tail_type

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
                        let generator = gf.MakeGenerator scope_subctx form
                        let local_type = List.head (generator.ReturnTypes())
                        let local = ilGen.DeclareLocal(local_type)
                        scope_subctx.locals.[name] <- Local (local, local_type)
                        generator.Generate ilGen
                        ilGen.Emit (OpCodes.Stloc,local)
                    | other -> failwithf "In let bindings: Expected: (name (form))\nGot: %A\n" other
            | other -> failwithf "In let form: expected: list of bindings\nGot: %A" other
            let bodyGen = (new BodyGenerator (scope_subctx,typeBuilder,body,gf) :> IGenerator)
            bodyGen.Generate ilGen
            ilGen.EndScope()
        member this.ReturnTypes () =
            let type_subctx = new Context(context)
            match bindings with
            | List list ->
                for binding in list do
                    match binding with
                    | List [(Atom (Symbol name)); form] ->
                        let generator = gf.MakeGenerator type_subctx form
                        type_subctx.locals.[name] <- Local (null,generator.ReturnTypes() |> List.head)
                    | other -> failwithf "In let bindings: Expected: (name (form))\nGot: %A\n" other
            | other -> failwithf "In let form: expected: list of bindings\nGot: %A" other
            (gf.MakeBody type_subctx body).ReturnTypes()

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
        member this.ReturnTypes () =
            (gf.MakeGenerator context if_true).ReturnTypes()

type FullIfGenerator(context:Context,typeBuilder:TypeBuilder,condition:SExp,if_true:SExp,if_false:SExp,gf:IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            let cond_gen = gf.MakeGenerator context condition
            let if_true_gen = gf.MakeGenerator context if_true
            let if_false_gen = gf.MakeGenerator context if_false
            let if_true_lbl = ilGen.DefineLabel()
            let end_form = ilGen.DefineLabel()
            ignore (cond_gen.Generate ilGen)
            ilGen.Emit (OpCodes.Brtrue, if_true_lbl)
            if_false_gen.Generate ilGen
            ilGen.Emit (OpCodes.Br,end_form)
            ilGen.MarkLabel if_true_lbl
            if_true_gen.Generate ilGen
            ilGen.MarkLabel end_form
        member this.ReturnTypes () =
            let true_ret_type = (gf.MakeGenerator context if_true).ReturnTypes()
            let false_ret_type = (gf.MakeGenerator context if_false).ReturnTypes()
            List.concat (Seq.ofList [true_ret_type; false_ret_type]) //TODO This should return closest common ancestor of these types

type FunCallGenerator(context:Context,typeBuilder:TypeBuilder,fname:string,arguments:SExp list,gf:IGeneratorFactory) =
    let args_seq = gf.MakeSequence context arguments
    let func = context.functions.[fname] <| args_seq.ReturnTypes()
    interface IGenerator with
        member this.Generate ilGen =
            args_seq.Generate ilGen
            ilGen.Emit(OpCodes.Call,func)
        member this.ReturnTypes () =
            [func.ReturnType]

type DefunGenerator(context:Context,typeBuilder:TypeBuilder,fname:string,parameters:SExp list,body:SExp list,gf:IGeneratorFactory) =
    do context.functions.[fname] <- (fun arg_types ->
                                            let methodGen = typeBuilder.DefineMethod(fname, MethodAttributes.Public ||| MethodAttributes.Static, typeof<obj>, (Array.ofList arg_types))
                                            let methodILGen = (methodGen.GetILGenerator())
                                            let fun_ctx = new Context(context)
                                            for parm in parameters do
                                                match parm with
                                                | Atom(Symbol parm_name) ->
                                                    let parm_idx = (List.findIndex (fun (p) -> p = parm) parameters)
                                                    fun_ctx.locals.[parm_name] <- Arg (parm_idx,arg_types.[parm_idx])
                                                | other -> failwithf "In function %A parameter definition:\nExpected: Atom(Symbol)\nGot: %A" fname parm
                                            let bodyGen = gf.MakeBody fun_ctx body
                                            bodyGen.Generate methodILGen
                                            methodILGen.Emit(OpCodes.Ret)
                                            methodGen :> MethodInfo)
    interface IGenerator with
        member this.Generate ilGen =
            ()
        member  this.ReturnTypes() = 
            [typeof<Void>]

type QuoteGenerator(context:Context,typeBuilder:TypeBuilder,quotedExp:SExp,gf:IGeneratorFactory) =
    let generate_object (ilGen:ILGenerator) (o:obj) =
        let generator = gf.MakeGenerator context (Atom (Object o))
        generator.Generate ilGen
    let generate_symbol (ilGen:ILGenerator) (name:string) =
        let cons = (typeof<Naggum.Runtime.Symbol>).GetConstructor [|typeof<string>|]
        ilGen.Emit(OpCodes.Ldstr,name)
        ilGen.Emit(OpCodes.Newobj,cons)
    let rec generate_list (ilGen:ILGenerator) (elements:SExp list) =
        let generate_list_element e =
            match e with
            | List l -> generate_list ilGen l
            | Atom (Object o) -> generate_object ilGen o
            | Atom (Symbol s) -> generate_symbol ilGen s
            | other -> failwithf "Error: Unexpected form in quoted expression: %A" other
        let cons = (typeof<Naggum.Runtime.Cons>).GetConstructor(Array.create 2 typeof<obj>)
        List.rev elements |> List.head |> generate_list_element //last element
        ilGen.Emit(OpCodes.Ldnull) //list terminator
        ilGen.Emit(OpCodes.Newobj,cons)
        List.rev elements |> List.tail |> List.iter (fun (e) ->
                                                        generate_list_element e
                                                        ilGen.Emit(OpCodes.Newobj,cons))
    interface IGenerator with
        member this.Generate ilGen =
            match quotedExp with
            |List l -> generate_list ilGen l
            |Atom (Object o) -> generate_object ilGen o
            |Atom (Symbol s) -> generate_symbol ilGen s
        member this.ReturnTypes () =
            match quotedExp with
            |List l -> [typeof<Naggum.Runtime.Cons>]
            |Atom (Object o) -> [typeof<System.Object>]
            |Atom (Symbol s) -> [typeof<Naggum.Runtime.Symbol>]

type NewObjGenerator(context : Context, typeBuilder : TypeBuilder, typeName : string, arguments : SExp list, gf : IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            let args_gen = gf.MakeSequence context arguments
            let argTypes = args_gen.ReturnTypes()
            let objType = 
                 if typeName.StartsWith "System" then
                    Type.GetType typeName
                 else
                    context.types.[typeName]
            let arg_types = args_gen.Generate ilGen
            ilGen.Emit(OpCodes.Newobj,objType.GetConstructor(Array.ofList argTypes))
        member this.ReturnTypes () =
            if typeName.StartsWith "System" then
                [Type.GetType typeName]
            else
                [context.types.[typeName]]