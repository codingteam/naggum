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

module Naggum.Compiler.Reader

open System.IO
open FParsec.CharParsers

open System
open System.IO
open FParsec

type Value =
    |Object of obj
    |Symbol of string

type SExp =
    | Atom of Value
    | List of SExp list

let ws parser = parser .>> spaces
let list,listRef = createParserForwardedToRef()
let float = pfloat |>> (fun (flt) -> flt :> obj)
let int = pint32 |>> (fun (int) -> int :> obj)
let number = int <|> float |>> Object
let string =
    let normalChar = satisfy (fun c -> c <> '\"')
    between (pstring "\"")(pstring "\"") (manyChars normalChar) |>> (fun (str) -> str :> obj) |>> Object
let symChars = (anyOf "+-*/=<>!?.") //chars that are valid in the symbol name
let symbol = (many1Chars (letter <|> digit <|> symChars)) |>> Symbol

let atom =  (number <|> string <|> symbol) |>> Atom

let listElement = choice [atom;list]
let sexp = ws (pstring "(") >>. many (ws listElement) .>> ws (pstring ")") |>> List
let parser = choice [atom;sexp]
do listRef := sexp

let rec read_form (stream : TextReader) (acc:string) balance =
    let line = stream.ReadLine()
    let delta = balance
    String.iter (fun (c) ->
                    match (c) with
                    |'(' -> delta := !delta + 1
                    |')' -> delta := !delta - 1
                    |_ -> delta := !delta)
                line
    if !delta = 0 && not(acc.Trim() = "")then
        String.concat " " [acc;line]
    else read_form stream (String.concat " " [acc; line]) delta

let read stream =
    let form = (read_form stream "" (ref 0)).Trim()
    run parser form

let parse (source : StreamReader) =
    let form = read source
    match form with
    | Success(result,   _, _) -> result
    | Failure(errorMsg, _, _) -> failwithf "Failure: %s" errorMsg
