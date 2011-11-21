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

module Naggum.Interactive

open System
open FParsec
open Types
open Context
open Runtime

let context = Context []

Runtime.load context

let apply (context:Context) (fname_sexp:SExp) (args:SExp) =
    let fname = match fname_sexp with
                | Atom (Symbol name) -> Symbol name
                | any -> 
                    eprintfn "Expected: Atom (Symbol)\nGot: %A" any
                    raise (new ArgumentException())
    let func = match context.get fname with
               | Some (Function f) -> f
               | Some (Value v) -> 
                    eprintfn "Expected: Function\nGot: %A" (Value v)
                    raise (new ArgumentException())
               | None -> raise (new ArgumentException())
    func args

let eval_atom (context:Context) atom =
    match atom with
    | Atom (Symbol name) -> 
        match context.get (Symbol name) with
        | Some (Value value) ->Atom value
        | Some (Function _) -> 
            eprintf "Failure: procedural values not supported yet."
            raise (new ArgumentException())
        | None -> 
            eprintf "Failure: %A not bound." atom
            raise (new ArgumentException())
    | _ -> atom //should not be matched, anyway

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
                                                            fctx.add s (Value value)) args parms
                                        List (List.map (eval fctx) body)
                                    else
                                        eprintf "Funcntion %A expects %A args, received %A" fname arity (List.length args)
                                        raise (new  ArgumentException())
                                | _ ->
                                    eprintf "Function %A expected a list of arguments, received %A" fname sexp
                                    raise (new  ArgumentException())))

let eval_if (ctx:Context) eval condition if_true if_false =
    match (eval ctx condition) with
    | Atom (Symbol "nil") -> eval ctx if_false
    | List [] -> eval ctx if_false
    | _ -> eval ctx if_true

let eval_list (context:Context) eval list =
    match list with
    |List [] -> List [] //Special case for empty list
    // TODO differentiate this with branches to eval_let, eval_lambda, etc
    |List ( Atom (Symbol "defun") :: Atom fname :: List llist :: body) -> 
        eval_defun context eval fname llist body
        Atom fname
    |List (Atom (Symbol "if") :: condition :: if_true :: if_false :: []) -> eval_if context eval condition if_true if_false
    |List (Atom (Symbol "if") :: condition :: if_true :: []) -> eval_if context eval condition if_true (List [])
    |List list -> apply context (List.head list) (List (List.map (eval context) (List.tail list)))
    | _ -> list

let eval_quote quote =
    match quote with
    |Quote (List []) -> List [] //special case for empty quoted list
    |Quote literal -> literal
    | _ -> quote //should not be matched, anyway

let rec eval context sexp =
    match sexp with
    |Atom _ -> eval_atom context sexp
    |List _ -> eval_list context eval sexp
    |Quote _ -> eval_quote sexp

let ws parser = parser .>> spaces
let list,listRef = createParserForwardedToRef()
let number = pfloat |>> Number
let string =
    let normalChar = satisfy (fun c -> c <> '\"')
    between (pstring "\"")(pstring "\"") (manyChars normalChar) |>> String
let symbol = (many1Chars (letter <|> digit <|> (pchar '-'))) |>> Symbol

let atom =  (number <|> string <|> symbol) |>> Atom
let quote = (pstring "'") >>. choice [atom;list] |>> Quote
let listElement = choice [atom;list;quote]
let sexp = ws (pstring "(") >>. many (ws listElement) .>> ws (pstring ")") |>> List
let parser = choice [atom;quote;sexp]
do listRef := sexp

let parse p str =
    let parse_result = run p str
    parse_result

while true do
    Console.Out.Write "> "
    let expression = Console.In.ReadLine()
    match (parse parser expression) with
    | Success(result, _, _)   -> printfn "Success:\n Form:\n%A\n Result:\n%A" result (eval context result)
    | Failure(errorMsg, _, _) -> printfn "Failure: %s" errorMsg