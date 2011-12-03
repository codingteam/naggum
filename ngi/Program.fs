(*  Copyright (C) 2011 by Hagane, ForNeVeR

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

module Naggum.Interactive

open System
open Types
open Context
open FParsec
open Naggum.Runtime
open Naggum.Reader

let context = Context []

Runtime.load context

let undiscern list =
    List.map
        (function
            |Atom a -> a
            |Cons c -> c :> obj)
        list

let apply (context:Context) (fname:Symbol) (args:obj list) =
    let func = context.get fname
    match func with
    | None -> 
        eprintfn "Symbol %A is not bound." (fname.GetName())
        null
    | Some (Value _) ->
        eprintfn "%A is not a function." (fname.GetName())
        null
    | Some (Function f) -> f args


//Evaluates list exp
let eval_list context sexp =
    let head = List.head sexp
    let tail = List.tail sexp
    if is_symbol head then
        apply context (unbox head) tail
    else
        eprintfn "Not a symbol: %A" head
        null

//generic evaluation
let rec eval context sexp =
    match sexp with
    |Cons list -> eval_list context (undiscern list)

while true do
    Console.Out.Write "> "
    let read = read Console.In
    let expression = read ()
    match expression with
    | Success(result, _, _)   -> printfn "Success:\n Form:\n%A\n Result:\n%A" result (eval context result)
    | Failure(errorMsg, _, _) -> printfn "Error:\n %A" errorMsg