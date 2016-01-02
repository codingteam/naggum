module Naggum.Compiler.Context

open System
open System.Collections.Generic
open System.Reflection
open System.Reflection.Emit

open Naggum.Runtime
open Naggum.Compiler.Reader

type ContextValue =
    |Local of LocalBuilder * Type
    |Field of FieldBuilder * Type
    |Arg of int * Type

type Context =
    val types : Dictionary<Symbol,Type>
    val functions : Dictionary<Symbol, (Type list -> MethodInfo)>
    val locals : Dictionary<Symbol,ContextValue>
    new (t,f,l) =
        {types = t; functions = f; locals = l}
    new (ctx : Context) =
        let t = new Dictionary<Symbol, Type>(ctx.types)
        let f = new Dictionary<Symbol, (Type list -> MethodInfo)>(ctx.functions)
        let l = new Dictionary<Symbol,ContextValue>(ctx.locals)
        new Context (t,f,l)
    new() =
        let t = new Dictionary<Symbol, Type>()
        let f = new Dictionary<Symbol, (Type list -> MethodInfo)>()
        let l = new Dictionary<Symbol,ContextValue>()
        new Context (t,f,l)

    member public this.loadAssembly(asm:Assembly) =
        let types = List.ofArray (asm.GetTypes())
        List.iter (fun (t:Type) -> this.types.Add(new Symbol(t.FullName),t)) types

    member public this.captureLocal(localName: Symbol, typeBuilder: TypeBuilder) =
        let local = this.locals.[localName]
        match local with
        | Local (_,t) ->
                let field = typeBuilder.DefineField(localName.Name,t,FieldAttributes.Static ||| FieldAttributes.Private)
                this.locals.[localName] <- Field (field, t)
        | Field (fb,_) -> ()
        | Arg (_,_) -> failwithf "Unable to capture parameter %A" localName.Name
        
let create () =
    let context = new Context()
    context
