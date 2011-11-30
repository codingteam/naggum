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
open Naggum.Error

let context = Context []

Runtime.load context

//Applies a function to parameters in given context
let apply (context:Context) (fname_sexp:SExp) (args:SExp) =
    let fname = match fname_sexp with
                | Atom (Symbol name) -> Symbol name
                | any -> 
                    raise (error (Symbol "expression-error") (sprintf "Expected: Function\nGot: %A" any))
    let func = match context.get fname with
               | Some (Function f) -> f
               | Some (Value v) -> 
                    raise (error (Symbol "value-error")(sprintf "Expected: Function\nGot: %A" (Value v)))
               | None -> raise (new ArgumentException())
    func args

//Evaluates raw atom, like symbol or number
let eval_atom (context:Context) atom =
    match atom with
    | Atom (Symbol name) -> 
        match context.get (Symbol name) with
        | Some (Value value) ->Atom value
        | Some (Function _) -> 
            raise (error (Symbol "unsupported-operation") (sprintf "Failure: procedural values not supported yet."))
        | None -> 
            raise (error (Symbol "context-error") (sprintf "Failure: %A not bound." atom))
    | _ -> atom //should not be matched, anyway

//Evaluates defun form
let eval_defun (gctx:Context) eval fname llist body =
    let arity = List.length llist
    let args = List.map (function |Atom v -> v| _ -> Symbol "nil") llist
    gctx.add fname (Function (fun (sexp) ->
                                match sexp with
                                | List parms ->
                                    if (List.length args) = arity then
                                        let fctx = Context gctx.list
                                        List.iter2 (fun s v -> 
                                                        match v with
                                                        | Atom value ->
                                                            fctx.add s (Value value)
                                                        | any ->
                                                            raise (error (Symbol "call-error") (sprintf "Unexpected %A in call to %A" any fname)))
                                                        args parms
                                        List (List.map (eval fctx) body)
                                    else
                                        raise (error (Symbol "call-error") (sprintf "Funcntion %A expects %A arguments, received %A" fname arity (List.length args)))
                                | _ ->
                                    raise (error (Symbol "call-error") (sprintf "Function %A expected a list of arguments, received %A" fname sexp))))

//Evaluates if form 
let eval_if (ctx:Context) eval condition if_true if_false =
    match (eval ctx condition) with
    | Atom (Symbol "nil") -> eval ctx if_false
    | List [] -> eval ctx if_false
    | _ -> eval ctx if_true

///Evaluates let form
let eval_let (gctx:Context) eval llist body =
    let letctx = Context gctx.list
    match llist with
    | List binds -> List.iter (fun (bind) ->
                                match bind with
                                | List [Atom(Symbol name); bindval] ->
                                    letctx.add (Symbol name)
                                               (match (eval gctx bindval) with
                                                | Atom a -> Value a
                                                | List _ -> Value EmptyList
                                                | Quote q -> Value EmptyList)
                                | any ->
                                    raise (error (Symbol "expression-error") (sprintf "Improper let binding: %A" any)))
                              binds
                    List (List.map (eval letctx) body)
    | any -> 
        raise (error (Symbol "expression-error") (sprintf "Expected: List\nGot: %A" any))                

//Evaluates list exp
let eval_list (context:Context) eval list =
    match list with
    |List [] -> List [] //Special case for empty list
    // TODO differentiate this with branches to eval_let, eval_lambda, etc
    |List ( Atom (Symbol "defun") :: Atom fname :: List llist :: body) -> 
        eval_defun context eval fname llist body
        Atom fname
    |List (Atom (Symbol "if") :: condition :: if_true :: if_false :: []) -> eval_if context eval condition if_true if_false
    |List (Atom (Symbol "if") :: condition :: if_true :: []) -> eval_if context eval condition if_true (List [])
    |List (Atom (Symbol "let") :: llist :: body) -> eval_let context eval llist body
    |List list -> apply context (List.head list) (List (List.map (eval context) (List.tail list)))
    | _ -> list

//Evaluates quote exp
let eval_quote quote =
    match quote with
    |Quote (List []) -> List [] //special case for empty quoted list
    |Quote literal -> literal
    | _ -> quote //should not be matched, anyway

//generic evaluation
let rec eval context sexp =
    match sexp with
    |Atom _ -> eval_atom context sexp
    |List _ -> eval_list context eval sexp
    |Quote _ -> eval_quote sexp

while true do
    Console.Out.Write "> "
    let read = read Console.In
    try
        let expression = read ()
        match expression with
        | Success(result, _, _)   -> printfn "Success:\n Form:\n%A\n Result:\n%A" result (eval context result)
        | Failure(errorMsg, _, _) -> raise (error (Symbol "parser-error") (sprintf "Failure: %s" errorMsg))
    with
    | :? NaggumError as ex -> print_error ex