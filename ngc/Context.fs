(*  Copyright (C) 2011 by ForNeVeR,Hagane

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

open System.Collections.Generic
open System.Reflection
open System.Reflection.Emit

open Naggum.Runtime
open Naggum.Writer
open Naggum.Types.Cons

type Context =
    val functions : Dictionary<string, MethodInfo>
    val locals : Dictionary<string,LocalBuilder>
    new (f,l) =
        { functions = f; locals = l}
    new (ctx : Context) =
        let f = new Dictionary<string, MethodInfo>(ctx.functions)
        let l = new Dictionary<string,LocalBuilder>(ctx.locals)
        new Context (f,l)
    new() =
        let f = new Dictionary<string, MethodInfo>()
        let l = new Dictionary<string,LocalBuilder>()
        new Context (f,l)

let create () =
    let context = new Context()
    context.functions.["add"]   <- typeof<Math>.GetMethod "add"
    context.functions.["sub"]   <- typeof<Math>.GetMethod "sub"
    context.functions.["mul"]   <- typeof<Math>.GetMethod "mul"
    context.functions.["div"]   <- typeof<Math>.GetMethod "div"
    context.functions.["equal"] <- typeof<Math>.GetMethod "equal"
    context.functions.["write"] <- typeof<Writer>.GetMethod "write"
    context.functions.["writeln"] <- typeof<Writer>.GetMethod "writeln"
    context.functions.["Cons"] <- typeof<Cons>.GetMethod "Cons"
    context.functions.["Car"] <- typeof<Cons>.GetMethod "Car"
    context.functions.["Cdr"] <- typeof<Cons>.GetMethod "Cdr"
    context
