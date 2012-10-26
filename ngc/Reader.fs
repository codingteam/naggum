(*  Copyright (C) 2011-2012 by ForNeVeR, Hagane

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

let comment = (skipChar ';') .>> (skipRestOfLine true)
let skipSpaceAndComments = skipMany (spaces1 <|> comment)
let commented parser = skipSpaceAndComments >>. parser .>> skipSpaceAndComments
let list,listRef = createParserForwardedToRef()

let numberOptions =
    NumberLiteralOptions.AllowMinusSign
    ||| NumberLiteralOptions.AllowExponent
    ||| NumberLiteralOptions.AllowHexadecimal
    ||| NumberLiteralOptions.AllowFraction
    ||| NumberLiteralOptions.AllowSuffix
let pnumber : Parser<Value,unit> =
    let pliteral = numberLiteral numberOptions "number"
    fun stream ->
        let reply = pliteral stream
        if reply.Status = Ok then
            let result : NumberLiteral = reply.Result
            if result.IsInteger then
                if result.SuffixLength = 1 && result.SuffixChar1 = 'L' then
                    Reply((int64 result.String) :> obj |> Object)
                else
                    if not (result.SuffixLength = 1) then
                        Reply((int32 result.String) :> obj |> Object)
                    else
                        Reply (ReplyStatus.Error, messageError <| sprintf "Unknown suffix: %A" result.SuffixChar1)
            else
                if result.SuffixLength = 1 && result.SuffixChar1 = 'f' then
                    Reply((float result.String) :> obj |> Object)
                else
                    if not (result.SuffixLength = 1) then
                        Reply((single result.String) :> obj |> Object)
                    else
                        Reply (ReplyStatus.Error, messageError <| sprintf "Unknown suffix: %A" result.SuffixChar1)
        else
            Reply(reply.Status,reply.Error)
let string =
    let normalChar = satisfy (fun c -> c <> '\"')
    between (pstring "\"")(pstring "\"") (manyChars normalChar) |>> (fun (str) -> str :> obj) |>> Object
let symChars = (anyOf "+-*/=<>!?.") //chars that are valid in the symbol name
let symbol = (many1Chars (letter <|> digit <|> symChars)) |>> Symbol

let atom =  (pnumber <|> string <|> symbol) |>> Atom

let listElement = choice [atom;list]
let sexp = commented (pchar '(') >>. many (commented listElement) .>> commented (pchar ')') |>> List
let parser = many1 (choice [atom;sexp])
do listRef := sexp

let parse (sourceName:string) (source : Stream) =
    let form = runParserOnStream parser () sourceName source Text.Encoding.UTF8
    match form with
    | Success(result,   _, _) -> result
    | Failure(errorMsg, _, _) -> failwithf "Failure: %s" errorMsg
