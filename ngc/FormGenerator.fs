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
                    |Local (local, t) ->
                        ilGen.Emit(OpCodes.Ldloc,local)
                        [t]
                    |Arg index ->
                        ilGen.Emit(OpCodes.Ldarg,(int16 index))
                        [typeof<obj>]
                with
                | :? KeyNotFoundException -> failwithf "Symbol %A not bound." name

type SequenceGenerator(context:Context,typeBuilder:TypeBuilder,seq:SExp list, gf:IGeneratorFactory) =
    member private this.gen_seq (ilGen:ILGenerator,seq:SExp list) =
        match seq with
            | [] -> 
                ilGen.Emit(OpCodes.Ldnull)
                [typeof<Void>]
            | [last] ->
                let gen = gf.MakeGenerator context last
                gen.Generate ilGen
            | sexp :: rest ->
                let gen = gf.MakeGenerator context sexp
                ignore (gen.Generate ilGen)
                this.gen_seq (ilGen, rest)
    interface IGenerator with
        member this.Generate ilGen = this.gen_seq (ilGen,seq)

type BodyGenerator(context:Context,typeBuilder:TypeBuilder,body:SExp list, gf:IGeneratorFactory) =
    member private this.gen_body (ilGen:ILGenerator,body:SExp list) =
        match body with
            | [] ->
                ilGen.Emit(OpCodes.Ldnull)
                [typeof<Void>]
            | [last] ->
                let gen = gf.MakeGenerator context last
                let val_type = gen.Generate ilGen
                if (val_type = [typeof<System.Void>]) then
                    ilGen.Emit(OpCodes.Ldnull)
                val_type
            | sexp :: rest ->
                let gen = gf.MakeGenerator context sexp
                let val_type = gen.Generate ilGen
                if not (val_type = [typeof<System.Void>]) then
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
                        let local = ilGen.DeclareLocal(typeof<obj>)
                        let generator = gf.MakeGenerator scope_subctx form
                        let local_type = List.head (generator.Generate ilGen)
                        scope_subctx.locals.[name] <- Local (local, local_type)
                        ilGen.Emit (OpCodes.Stloc,local)
                    | other -> failwithf "In let bindings: Expected: (name (form))\nGot: %A\n" other
            | other -> failwithf "In let form: expected: list of bindings\nGot: %A" other
            let bodyGen = (new BodyGenerator (scope_subctx,typeBuilder,body,gf) :> IGenerator)
            let return_type = bodyGen.Generate ilGen
            ilGen.EndScope()
            return_type

type ReducedIfGenerator(context:Context,typeBuilder:TypeBuilder,condition:SExp,if_true:SExp,gf:IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            let cond_gen = gf.MakeGenerator context condition
            let if_true_gen = gf.MakeGenerator context if_true
            let if_true_lbl = ilGen.DefineLabel()
            let end_form = ilGen.DefineLabel()
            ignore (cond_gen.Generate ilGen)
            ilGen.Emit (OpCodes.Brtrue, if_true_lbl)
            ilGen.Emit OpCodes.Ldnull
            ilGen.Emit (OpCodes.Br,end_form)
            ilGen.MarkLabel if_true_lbl
            let return_type = if_true_gen.Generate ilGen
            ilGen.MarkLabel end_form
            return_type

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
            let false_ret_type = if_false_gen.Generate ilGen
            ilGen.Emit (OpCodes.Br,end_form)
            ilGen.MarkLabel if_true_lbl
            let true_ret_type = if_true_gen.Generate ilGen
            ilGen.MarkLabel end_form
            List.concat (Seq.ofList [true_ret_type; false_ret_type]) //TODO This should return closest common ancestor of these types

type FunCallGenerator(context:Context,typeBuilder:TypeBuilder,fname:string,arguments:SExp list,gf:IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            let func = context.functions.[fname]
            let args_seq = gf.MakeSequence context arguments
            let arg_types = args_seq.Generate ilGen
            ilGen.Emit(OpCodes.Call,func)
            [func.ReturnType]

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
            let body_ret_type = bodyGen.Generate methodILGen
            methodILGen.Emit(OpCodes.Ret)
            [typeof<Void>]

type QuoteGenerator(context:Context,typeBuilder:TypeBuilder,quotedExp:SExp,gf:IGeneratorFactory) =
    let generate_object (ilGen:ILGenerator) (o:obj) =
        let generator = gf.MakeGenerator context (Atom (Object o))
        generator.Generate ilGen
    let generate_symbol (ilGen:ILGenerator) (name:string) =
        let cons = (typeof<Naggum.Runtime.Symbol>).GetConstructor [|typeof<string>|]
        ilGen.Emit(OpCodes.Ldstr,name)
        ilGen.Emit(OpCodes.Newobj,cons)
        [typeof<Naggum.Runtime.Symbol>]
    let rec generate_list (ilGen:ILGenerator) (elements:SExp list) =
        let cons = (typeof<Naggum.Runtime.Cons>).GetConstructor(Array.create 2 typeof<obj>)
        ilGen.Emit(OpCodes.Ldnull) //list terminator
        List.iter (fun (e) ->
                        match e with
                        |List l -> ignore (generate_list ilGen l)
                        |Atom (Object o) -> ignore (generate_object ilGen o)
                        |Atom (Symbol s) -> ignore (generate_symbol ilGen s)
                        ilGen.Emit(OpCodes.Newobj,cons))
                  (List.rev elements)
        [typeof<Naggum.Runtime.Cons>]
    interface IGenerator with
        member this.Generate ilGen =
            match quotedExp with
            |List l -> generate_list ilGen l
            |Atom (Object o) -> generate_object ilGen o
            |Atom (Symbol s) -> generate_symbol ilGen s

type ClrCallGenerator(context : Context, typeBuilder : TypeBuilder, clrType : Type, methodName : string, arguments : SExp list,
                      gf : IGeneratorFactory) =
    let nearestOverload (clrType : Type) methodName types =
        let rec distanceBetweenTypes (derivedType : Type, baseType) =
            match derivedType with
            | null                     -> None
            | someType
              when someType = baseType -> Some 0
            | _                        ->
                maybe {
                    let! distance = distanceBetweenTypes (derivedType.BaseType, baseType)
                    return distance + 1
                }
        let distance (availableTypes : Type list) (methodTypes : Type list) =
            if availableTypes.Length <> methodTypes.Length then
                None
            else
                Seq.zip methodTypes availableTypes
                |> Seq.map distanceBetweenTypes
                |> Seq.fold (fun state option ->
                                maybe {
                                    let! stateNum = state
                                    let! optionNum = option
                                    return stateNum + optionNum
                                }) (Some 0)
        let methods = clrType.GetMethods() |> Seq.filter (fun clrMethod -> clrMethod.Name = methodName)
        let methodsAndDistances = methods
                                  |> Seq.map (fun clrMethod -> clrMethod,
                                                               distance types (clrMethod.GetParameters()
                                                                               |> Array.map (fun parameter ->
                                                                                             parameter.ParameterType)
                                                                               |> Array.toList))
                                  |> Seq.filter (snd >> Option.isSome)
                                  |> Seq.map (fun (clrMethod, distance) -> clrMethod, Option.get distance)
                                  |> Seq.toList
        if methodsAndDistances.IsEmpty then
            None
        else
            let minDistance = methodsAndDistances |> List.minBy snd |> snd
            let methods = methodsAndDistances |> List.filter (snd >> (fun d -> d = minDistance))
                                              |> List.map fst
            if methods.IsEmpty then
                None
            else
                Some (List.head methods)
    
    interface IGenerator with
        member this.Generate ilGen =
            let argTypes = arguments
                           |> List.map (fun sexp -> match sexp with
                                                    | Atom (Object arg) -> arg.GetType()
                                                    | Atom (Symbol _)   -> typeof<obj>
                                                    | List _            -> typeof<obj>
                                                    | any               -> failwithf "Cannot use %A in CLR call." any)
            let clrMethod = nearestOverload clrType methodName argTypes
            let args_seq = gf.MakeSequence context arguments
            let arg_types = args_seq.Generate ilGen            
            ilGen.Emit(OpCodes.Call, Option.get clrMethod)
            if not ((Option.get clrMethod).ReturnType = typeof<System.Void>) then
                ilGen.Emit(OpCodes.Ldnull)
            [(Option.get clrMethod).ReturnType]

type NewObjGenerator(context : Context, typeBuilder : TypeBuilder, typeName : string, arguments : SExp list, gf : IGeneratorFactory) =
    interface IGenerator with
        member this.Generate ilGen =
            let argTypes = arguments
                           |> List.map (fun sexp -> match sexp with
                                                    | Atom (Object arg) -> arg.GetType()
                                                    | any               -> failwithf "Cannot use %A in CLR call." any)
            let args_gen = gf.MakeSequence context arguments
            let objType = context.types.[typeName]
            let arg_types = args_gen.Generate ilGen
            ilGen.Emit(OpCodes.Newobj,objType.GetConstructor(Array.ofList argTypes))
            [objType]