module Naggum.Compiler.FormGenerator

open System
open System.Collections.Generic
open System.Reflection
open System.Reflection.Emit
open Naggum.Runtime
open Naggum.Compiler.Context
open Naggum.Compiler.IGenerator
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
                let ctxval = context.locals.[new Symbol(name)]
                match ctxval with
                |Local (local, _) ->
                    ilGen.Emit(OpCodes.Ldloc,local)
                |Arg (index,_) ->
                    ilGen.Emit(OpCodes.Ldarg,(int16 index))
            with
            | :? KeyNotFoundException -> failwithf "Symbol %A not bound." name
        member this.ReturnTypes () =
            match context.locals.[new Symbol(name)] with
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

type BodyGenerator(context : Context,
                   methodBuilder : MethodBuilder,
                   body : SExp list,
                   gf : IGeneratorFactory) =
    let rec genBody (ilGen : ILGenerator) (body : SExp list) =
        match body with
            | [] ->
                ilGen.Emit(OpCodes.Ldnull)
            | [last] ->
                let gen = gf.MakeGenerator context last
                let stackType = List.head <| gen.ReturnTypes ()
                let returnType = methodBuilder.ReturnType
                gen.Generate ilGen
                match (stackType, returnType) with
                | (s, r) when s = typeof<Void> && r = typeof<Void> -> ()
                | (s, r) when s = typeof<Void> && r <> typeof<Void> -> ilGen.Emit OpCodes.Ldnull
                | (s, r) when s <> typeof<Void> && r = typeof<Void> -> ilGen.Emit OpCodes.Pop
                | _ -> ()
            | sexp :: rest ->
                let gen = gf.MakeGenerator context sexp
                let val_type = gen.ReturnTypes()
                gen.Generate ilGen
                if List.head val_type <> typeof<Void> then
                    ilGen.Emit(OpCodes.Pop)
                genBody ilGen rest
    interface IGenerator with
        member __.Generate ilGen = 
            genBody ilGen body
        member this.ReturnTypes () =
            match body with
            |[] -> [typeof<System.Void>]
            |somelist -> 
                let tail_type = (gf.MakeGenerator context (List.rev body |> List.head)).ReturnTypes()
                if tail_type = [typeof<System.Void>] then
                    [typeof<obj>]
                else tail_type

type LetGenerator(context : Context,
                  typeBuilder : TypeBuilder,
                  methodBuilder : MethodBuilder,
                  bindings:SExp,
                  body : SExp list,
                  gf : IGeneratorFactory) =
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
                        scope_subctx.locals.[new Symbol(name)] <- Local (local, local_type)
                        generator.Generate ilGen
                        ilGen.Emit (OpCodes.Stloc,local)
                    | other -> failwithf "In let bindings: Expected: (name (form))\nGot: %A\n" other
            | other -> failwithf "In let form: expected: list of bindings\nGot: %A" other
            let bodyGen = new BodyGenerator (scope_subctx, methodBuilder, body, gf) :> IGenerator
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
                        type_subctx.locals.[new Symbol(name)] <- Local (null,generator.ReturnTypes() |> List.head)
                    | other -> failwithf "In let bindings: Expected: (name (form))\nGot: %A\n" other
            | other -> failwithf "In let form: expected: list of bindings\nGot: %A" other
            (gf.MakeBody type_subctx body).ReturnTypes()

type ReducedIfGenerator(context:Context,typeBuilder:TypeBuilder,condition:SExp,if_true:SExp,gf:IGeneratorFactory) =
    let returnTypes = (gf.MakeGenerator context if_true).ReturnTypes()
    interface IGenerator with
        member this.Generate ilGen =
            let cond_gen = gf.MakeGenerator context condition
            let if_true_gen = gf.MakeGenerator context if_true
            let if_true_lbl = ilGen.DefineLabel()
            let end_form = ilGen.DefineLabel()
            cond_gen.Generate ilGen
            ilGen.Emit (OpCodes.Brtrue, if_true_lbl)
            
            if List.head returnTypes <> typeof<Void>
            then ilGen.Emit OpCodes.Ldnull
            
            ilGen.Emit (OpCodes.Br, end_form)
            ilGen.MarkLabel if_true_lbl
            if_true_gen.Generate ilGen
            ilGen.MarkLabel end_form
        member this.ReturnTypes () =
            returnTypes

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
    let func = context.functions.[new Symbol(fname)] <| args_seq.ReturnTypes()
    interface IGenerator with
        member this.Generate ilGen =
            args_seq.Generate ilGen
            ilGen.Emit(OpCodes.Call,func)
        member this.ReturnTypes () =
            [func.ReturnType]

type DefunGenerator(context:Context,typeBuilder:TypeBuilder,fname:string,parameters:SExp list,body:SExp list,gf:IGeneratorFactory) =
    do context.functions.[new Symbol(fname)] <- (fun arg_types ->
                                            let methodGen = typeBuilder.DefineMethod(fname, MethodAttributes.Public ||| MethodAttributes.Static, typeof<obj>, (Array.ofList arg_types))
                                            let methodILGen = (methodGen.GetILGenerator())
                                            let fun_ctx = new Context(context)
                                            for parm in parameters do
                                                match parm with
                                                | Atom(Symbol parm_name) ->
                                                    let parm_idx = (List.findIndex (fun (p) -> p = parm) parameters)
                                                    fun_ctx.locals.[new Symbol(parm_name)] <- Arg (parm_idx,arg_types.[parm_idx])
                                                | other -> failwithf "In function %A parameter definition:\nExpected: Atom(Symbol)\nGot: %A" fname parm
                                            let methodFactory = gf.MakeGeneratorFactory typeBuilder methodGen
                                            let bodyGen = methodFactory.MakeBody fun_ctx body
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
                    context.types.[new Symbol(typeName)]
            let arg_types = args_gen.Generate ilGen
            ilGen.Emit(OpCodes.Newobj,objType.GetConstructor(Array.ofList argTypes))
        member this.ReturnTypes () =
            if typeName.StartsWith "System" then
                [Type.GetType typeName]
            else
                [context.types.[new Symbol(typeName)]]

type TypeGenerator(context : Context, typeBuilder : TypeBuilder, typeName : string, parentTypeName: string, members : SExp list, gf : IGeneratorFactory) =
    let newTypeBuilder = 
        if parentTypeName = "" then
            Globals.ModuleBuilder.DefineType(typeName, TypeAttributes.Class ||| TypeAttributes.Public, typeof<obj>)
        else
            Globals.ModuleBuilder.DefineType(typeName, TypeAttributes.Class ||| TypeAttributes.Public, context.types.[new Symbol(parentTypeName)])
    let mutable fields : string list = []

    let generate_field field_name =
        let fieldBuilder = newTypeBuilder.DefineField(field_name,typeof<obj>,FieldAttributes.Public)
        fields <- List.append fields [field_name]
    let generate_method method_name method_parms method_body =
        let method_gen = newTypeBuilder.DefineMethod(method_name,MethodAttributes.Public,
                            typeof<obj>,
                            Array.create (List.length method_parms) typeof<obj>)
        let method_ctx = new Context(context)
        for parm in method_parms do
            match parm with
            | Atom(Symbol parm_name) ->
                let parm_idx = (List.findIndex (fun (p) -> p = parm) method_parms)
                method_ctx.locals.[new Symbol(parm_name)] <- Arg (parm_idx,typeof<obj>)
            | other -> failwithf "In method %A%A parameter definition:\nExpected: Atom(Symbol)\nGot: %A" typeName method_name parm
        let newGeneratorFactory = gf.MakeGeneratorFactory newTypeBuilder method_gen
        let body_gen = newGeneratorFactory.MakeBody method_ctx method_body
        body_gen.Generate (method_gen.GetILGenerator())
        (method_gen.GetILGenerator()).Emit(OpCodes.Ret)

    interface IGenerator with
        member this.Generate ilGen = 
            for m in members do
                match m with
                | List (Atom (Symbol "field") :: Atom (Symbol name) :: []) -> generate_field name
                | List (Atom (Symbol "field") :: Atom (Symbol access) :: Atom (Symbol name) :: []) -> generate_field name
                | List (Atom (Symbol "method") :: Atom (Symbol name) :: List parms :: body) -> generate_method name parms body
                | List (Atom (Symbol "method") :: Atom (Symbol name) :: Atom (Symbol access) :: List parms :: body) -> generate_method name parms body
                | other -> failwithf "In definition of type %A: \nUnknown member definition: %A" typeName other
        member this.ReturnTypes () =
            [typeof<System.Void>]
