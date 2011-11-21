module Naggum.Runtime

open System
open Types
open Context

let add = fun (sexp) ->
            let args = List.map (fun (x) ->
                                    match x with
                                    |Atom (Number n) -> n
                                    |List ([Atom (Number n)]) -> n
                                    |any ->
                                        eprintfn "Expected: Number\nGot: %A" any
                                        raise (new ArgumentException()))
                                (match sexp with
                                    | List xs -> xs
                                    | Atom a -> [Atom a]
                                    | Quote q -> 
                                        eprintfn "Expected: Number\nGot: Quote %A" q
                                        raise (new ArgumentException()))
            List.reduce (+) args |> Number |> Atom

let sub = fun (sexp) ->
            let args = List.map (fun (x) ->
                                    match x with
                                    |Atom (Number n) -> n
                                    |List ([Atom (Number n)]) -> n
                                    |any ->
                                        eprintfn "Expected: Number\nGot: %A" any
                                        raise (new ArgumentException()))
                                (match sexp with
                                    | List xs -> xs
                                    | Atom a -> [Atom a]
                                    | Quote q -> 
                                        eprintfn "Expected: Number\nGot: Quote %A" q
                                        raise (new ArgumentException()))
            List.reduce (-) args |> Number |> Atom

let mul = fun (sexp) ->
            let args = List.map (fun (x) ->
                                    match x with
                                    |Atom (Number n) -> n
                                    |List ([Atom (Number n)]) -> n
                                    |any ->
                                        eprintfn "Expected: Number\nGot: %A" any
                                        raise (new ArgumentException()))
                                (match sexp with
                                    | List xs -> xs
                                    | Atom a -> [Atom a]
                                    | Quote q -> 
                                        eprintfn "Expected: Number\nGot: Quote %A" q
                                        raise (new ArgumentException()))
            List.reduce (*) args |> Number |> Atom

let div = fun (sexp) ->
            let args = List.map (fun (x) ->
                                    match x with
                                    |Atom (Number n) -> n
                                    |List ([Atom (Number n)]) -> n
                                    |any ->
                                        eprintfn "Expected: Number\nGot: %A" any
                                        raise (new ArgumentException()))
                                (match sexp with
                                    | List xs -> xs
                                    | Atom a -> [Atom a]
                                    | Quote q -> 
                                        eprintfn "Expected: Number\nGot: Quote %A" q
                                        raise (new ArgumentException()))
            List.reduce (/) args |> Number |> Atom

let equal = fun (sexp) ->
            let args =
                (match sexp with
                 | List xs -> xs
                 | Atom a -> [Atom a]
                 | Quote q -> [Quote q])
            if (List.forall (fun (elem) -> elem = args.[0]) args) then
                Atom (Symbol "t")
            else
                List []

let cons = fun (sexp) ->
            match sexp with
            | List [car;cdr] ->
                let car_val = match car with
                                | Atom a -> a
                                | any ->
                                    eprintfn "Expected: value\nGot %A\n" any
                                    raise (new ArgumentException())
                let cdr_val = match cdr with
                                | Atom a -> a
                                | any ->
                                    eprintfn "Expected: value\nGot %A\n" any
                                    raise (new ArgumentException())
                Cons (car_val,cdr_val) |> Atom
            | any -> 
                eprintfn "Expected: 2 values\nGot %A\n" any
                raise (new ArgumentException())

let car = fun (sexp) ->
            match sexp with
            | List [Atom (Cons (carval,_))] -> Atom carval
            | any ->
                eprintfn "Expected: Cons\nGot %A\n" any
                raise (new ArgumentException())

let cdr = fun (sexp) ->
            match sexp with
            | List [Atom (Cons (_,cdrval))] -> Atom cdrval
            | any ->
                eprintfn "Expected: Cons\nGot %A\n" any
                raise (new ArgumentException())

let load (ctx:Context) =
    ctx.add (Symbol "add") (Function add)
    ctx.add (Symbol "sub") (Function sub)
    ctx.add (Symbol "mul") (Function mul)
    ctx.add (Symbol "div") (Function div)
    ctx.add (Symbol "equal") (Function equal)
    ctx.add (Symbol "cons") (Function cons)
    ctx.add (Symbol "car") (Function car)
    ctx.add (Symbol "cdr") (Function cdr)