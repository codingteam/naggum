module Naggum.Compiler.FormGenerator

open System
open System.Collections.Generic
open System.Reflection
open System.Reflection.Emit

open GrEmit

open Naggum.Backend
open Naggum.Backend.Reader
open Naggum.Backend.Matchers
open Naggum.Runtime
open Naggum.Compiler.Context
open Naggum.Compiler.IGenerator

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
    inherit ValueGenerator(context, Reader.Symbol name)
    interface IGenerator with
        member __.Generate il =
            try
                let ctxval = context.locals.[Symbol(name)]
                match ctxval with
                | Local (local, _) -> il.Ldloc local
                | Arg (index, _) -> il.Ldarg index
            with
            | :? KeyNotFoundException -> failwithf "Symbol %A not bound." name

        member this.ReturnTypes () =
            match context.locals.[new Symbol(name)] with
            |Local (_,t) -> [t]
            |Arg (_,t) -> [t]

type SequenceGenerator (context : Context, seq : SExp list, gf : IGeneratorFactory) =
    let rec sequence (il : GroboIL) (seq : SExp list) =
        match seq with
            | [] ->
                ()
            | [last] ->
                let gen = gf.MakeGenerator context last
                gen.Generate il
            | sexp :: rest ->
                let gen = gf.MakeGenerator context sexp
                ignore (gen.Generate il)
                sequence il rest

    interface IGenerator with
        member __.Generate il = sequence il seq
        member this.ReturnTypes () =
            List.map (fun (sexp) -> List.head ((gf.MakeGenerator context sexp).ReturnTypes())) seq

type BodyGenerator(context : Context,
                   resultType : Type,
                   body : SExp list,
                   gf : IGeneratorFactory) =
    let rec genBody (il : GroboIL) (body : SExp list) =
        match body with
            | [] ->
                if resultType <> typeof<Void> then
                    il.Ldnull ()
            | [last] ->
                let gen = gf.MakeGenerator context last
                let stackType = List.head <| gen.ReturnTypes ()
                gen.Generate il
                match (stackType, resultType) with
                | (s, r) when s = typeof<Void> && r = typeof<Void> -> ()
                | (s, r) when s = typeof<Void> && r <> typeof<Void> -> il.Ldnull ()
                | (s, r) when s <> typeof<Void> && r = typeof<Void> -> il.Pop ()
                | _ -> ()
            | sexp :: rest ->
                let gen = gf.MakeGenerator context sexp
                let val_type = gen.ReturnTypes()
                gen.Generate il
                if List.head val_type <> typeof<Void> then
                    il.Pop ()
                genBody il rest

    interface IGenerator with
        member __.Generate ilGen =
            genBody ilGen body
        member __.ReturnTypes () =
            match body with
            | [] -> [typeof<System.Void>]
            | _ -> (gf.MakeGenerator context (List.last body)).ReturnTypes()

type LetGenerator (context : Context,
                   resultType : Type,
                   bindings:SExp,
                   body : SExp list,
                   gf : IGeneratorFactory) =
    interface IGenerator with
        member __.Generate il =
            let scope_subctx = new Context (context)
            match bindings with
            | List list ->
                for binding in list do
                    match binding with
                    | List [Symbol name; form] ->
                        let generator = gf.MakeGenerator scope_subctx form
                        let local_type = List.head (generator.ReturnTypes())
                        let local = il.DeclareLocal local_type
                        scope_subctx.locals.[new Symbol(name)] <- Local (local, local_type)
                        generator.Generate il
                        il.Stloc local
                    | other -> failwithf "In let bindings: Expected: (name (form))\nGot: %A\n" other
            | other -> failwithf "In let form: expected: list of bindings\nGot: %A" other
            let bodyGen = new BodyGenerator (scope_subctx, resultType, body, gf) :> IGenerator
            bodyGen.Generate il

        member this.ReturnTypes () =
            let type_subctx = new Context(context)
            match bindings with
            | List list ->
                for binding in list do
                    match binding with
                    | List [Symbol name; form] ->
                        let generator = gf.MakeGenerator type_subctx form
                        type_subctx.locals.[new Symbol(name)] <- Local (null,generator.ReturnTypes() |> List.head)
                    | other -> failwithf "In let bindings: Expected: (name (form))\nGot: %A\n" other
            | other -> failwithf "In let form: expected: list of bindings\nGot: %A" other
            (gf.MakeBody type_subctx body).ReturnTypes()

type ReducedIfGenerator (context : Context, condition : SExp, ifTrue : SExp, gf : IGeneratorFactory) =
    let returnTypes = (gf.MakeGenerator context ifTrue).ReturnTypes()
    interface IGenerator with
        member __.Generate il =
            let condGen = gf.MakeGenerator context condition
            let ifTrueGen = gf.MakeGenerator context ifTrue
            let ifTrueLbl = il.DefineLabel ("then", true)
            let endForm = il.DefineLabel ("endif", true)
            condGen.Generate il
            il.Brtrue ifTrueLbl

            if List.head returnTypes <> typeof<Void>
            then il.Ldnull ()

            il.Br endForm
            il.MarkLabel ifTrueLbl
            ifTrueGen.Generate il
            il.MarkLabel endForm

        member this.ReturnTypes () =
            returnTypes

type FullIfGenerator (context : Context, condition : SExp, ifTrue : SExp, ifFalse : SExp, gf : IGeneratorFactory) =
    interface IGenerator with
        member __.Generate il =
            let condGen = gf.MakeGenerator context condition
            let ifTrueGen = gf.MakeGenerator context ifTrue
            let ifFalseGen = gf.MakeGenerator context ifFalse
            let ifTrueLbl = il.DefineLabel ("then", true)
            let endForm = il.DefineLabel ("endif", true)
            ignore (condGen.Generate il)
            il.Brtrue ifTrueLbl
            ifFalseGen.Generate il
            il.Br endForm
            il.MarkLabel ifTrueLbl
            ifTrueGen.Generate il
            il.MarkLabel endForm

        member this.ReturnTypes () =
            let true_ret_type = (gf.MakeGenerator context ifTrue).ReturnTypes()
            let false_ret_type = (gf.MakeGenerator context ifFalse).ReturnTypes()
            List.concat (Seq.ofList [true_ret_type; false_ret_type]) //TODO This should return closest common ancestor of these types

type FunCallGenerator (context : Context, fname : string, arguments : SExp list, gf : IGeneratorFactory) =
    let args = gf.MakeSequence context arguments
    let func = context.functions.[new Symbol(fname)] <| args.ReturnTypes()
    interface IGenerator with
        member __.Generate il =
            args.Generate il
            il.Call func

        member this.ReturnTypes () =
            [func.ReturnType]

type DefunGenerator (context : Context,
                     typeBuilder : TypeBuilder,
                     fname : string,
                     parameters : SExp list,
                     body : SExp list,
                     gf : IGeneratorFactory) =
    let bodyGen argTypes =
        let methodGen = typeBuilder.DefineMethod(fname, MethodAttributes.Public |||  MethodAttributes.Static, typeof<obj>, (Array.ofList argTypes))
        use il = new GroboIL (methodGen)
        let fun_ctx = new Context(context)
        for parm in parameters do
            match parm with
            | Symbol paramName ->
                let parm_idx = (List.findIndex (fun (p) -> p = parm) parameters)
                fun_ctx.locals.[new Symbol(paramName)] <- Arg (parm_idx,argTypes. [parm_idx])
            | _ -> failwithf "In function %A parameter definition:\nExpected: Atom (Symbol)\nGot: %A" fname parm
        let methodFactory = gf.MakeGeneratorFactory typeBuilder methodGen
        let bodyGen = methodFactory.MakeBody fun_ctx body
        bodyGen.Generate il
        il.Ret ()
        methodGen :> MethodInfo
    do context.functions.[new Symbol(fname)] <- bodyGen

    interface IGenerator with
        member this.Generate ilGen =
            ()
        member  this.ReturnTypes() =
            [typeof<Void>]

type QuoteGenerator (context : Context, quotedExp : SExp, gf : IGeneratorFactory) =
    let generateObject (il : GroboIL) (o : obj) =
        let generator = gf.MakeGenerator context (Atom (Object o))
        generator.Generate il

    let generateSymbol (il : GroboIL) (name : string) =
        let cons = (typeof<Naggum.Runtime.Symbol>).GetConstructor [|typeof<string>|]
        il.Ldstr name
        il.Newobj cons

    let rec generateList (il : GroboIL) (elements : SExp list) =
        let generateListElement e =
            match e with
            | List l -> generateList il l
            | Object o -> generateObject il o
            | Symbol s -> generateSymbol il s
        let cons = (typeof<Naggum.Runtime.Cons>).GetConstructor(Array.create 2 typeof<obj>)
        List.last elements |> generateListElement
        il.Ldnull () //list terminator
        il.Newobj cons
        List.rev elements |> List.tail |> List.iter (fun (e) ->
                                                         generateListElement e
                                                         il.Newobj cons)

    interface IGenerator with
        member __.Generate il =
            match quotedExp with
            | List l -> generateList il l
            | Object o -> generateObject il o
            | Symbol s -> generateSymbol il s
        member this.ReturnTypes () =
            match quotedExp with
            | List _ -> [typeof<Naggum.Runtime.Cons>]
            | Object _ -> [typeof<System.Object>]
            | Symbol _ -> [typeof<Naggum.Runtime.Symbol>]

type NewObjGenerator (context : Context, typeName : string, arguments : SExp list, gf : IGeneratorFactory) =
    interface IGenerator with
        member __.Generate il =
            let argsGen = gf.MakeSequence context arguments
            let argTypes = argsGen.ReturnTypes()
            let objType =
                 if typeName.StartsWith "System" then
                    Type.GetType typeName
                 else
                    context.types.[new Symbol(typeName)]
            ignore <| argsGen.Generate il
            il.Newobj <| objType.GetConstructor(Array.ofList argTypes)

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
    let generateMethod method_name method_parms method_body =
        let methodGen = newTypeBuilder.DefineMethod(method_name,MethodAttributes.Public,
                            typeof<obj>,
                            Array.create (List.length method_parms) typeof<obj>)
        let context = new Context(context)
        for parm in method_parms do
            match parm with
            | Symbol paramName ->
                let parm_idx = (List.findIndex (fun (p) -> p = parm) method_parms)
                context.locals.[new Symbol(paramName)] <- Arg (parm_idx,typeof<obj>)
            | _ -> failwithf "In method %A%A parameter definition:\nExpected: Atom(Symbol)\nGot: %A" typeName method_name parm
        let newGeneratorFactory = gf.MakeGeneratorFactory newTypeBuilder methodGen
        let body_gen = newGeneratorFactory.MakeBody context method_body
        use il = new GroboIL (methodGen)
        body_gen.Generate il
        il.Ret ()

    interface IGenerator with
        member this.Generate ilGen =
            for m in members do
                match m with
                | List [Symbol "field"; Symbol name] -> generate_field name
                | List [Symbol "field"; Symbol access; Symbol name] -> generate_field name
                | List (Symbol "method" :: Symbol name :: List parms :: body) -> generateMethod name parms body
                | List (Symbol "method" :: Symbol name :: Symbol access :: List parms :: body) -> generateMethod name parms body
                | other -> failwithf "In definition of type %A: \nUnknown member definition: %A" typeName other
        member this.ReturnTypes () =
            [typeof<System.Void>]
