(*  Copyright (C) 2011-2012 by ForNeVeR,Hagane

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

module Naggum.Compiler.Context

open System
open System.Collections.Generic
open System.Reflection
open System.Reflection.Emit

open Naggum.Compiler.Reader

type ContextValue =
    |Local of LocalBuilder
    |Arg of int

type Context =
    val types : Dictionary<string,Type>
    val functions : Dictionary<string, MethodInfo>
    val locals : Dictionary<string,ContextValue>
    new (t,f,l) =
        {types = t; functions = f; locals = l}
    new (ctx : Context) =
        let t = new Dictionary<string, Type>(ctx.types)
        let f = new Dictionary<string, MethodInfo>(ctx.functions)
        let l = new Dictionary<string,ContextValue>(ctx.locals)
        new Context (t,f,l)
    new() =
        let t = new Dictionary<string, Type>()
        let f = new Dictionary<string, MethodInfo>()
        let l = new Dictionary<string,ContextValue>()
        new Context (t,f,l)

    member public this.loadAssembly(asm:Assembly) =
        let types = List.ofArray (asm.GetTypes())
        List.iter (fun (t:Type) -> this.types.Add(t.FullName,t)) types

let create () =
    let context = new Context()
    context
