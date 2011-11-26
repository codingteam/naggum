(*  Copyright (C) 2011 by Hagane

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

//TODO Move this file to common library, when we will have one.

module Naggum.Context

open Naggum.Types
open Naggum.Error

type ContextItem =
    |Value of Value
    |Function of (SExp -> SExp)

type Context(init) =
    let mutable objects:List<(string * ContextItem)> = init

    member public this.get symbol =
        match symbol with
        |Symbol name -> 
            try
                match List.find (fun (s,ci) -> s = name) objects with
                |(_,ci) -> Some ci
            with
                | :? System.Collections.Generic.KeyNotFoundException ->
                    None
        |any ->
            raise (error (Symbol "internal-error") (sprintf "Unable to get value from context:\nExpected: Symbol\nGot: %A" any))

    member public this.list =
        objects

    member public this.add symbol value =
        match symbol with
        |Symbol name -> objects <- List.append [(name,value)] objects
        |any -> raise (error (Symbol "internal-error") (sprintf "Unable to add value to context:\nExpected: Symbol\nGot: %A" any))

