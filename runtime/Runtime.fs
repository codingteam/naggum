module Naggum.Runtime

open System
open Types
open Error
open Context

let add = fun (sexp) ->
            let args = List.map (fun (x) ->
                                    match x with
                                    |Atom (Number n) -> n
                                    |List ([Atom (Number n)]) -> n
                                    |any ->
                                        raise (error (Symbol "parameter-error") (sprintf "Expected: Number\nGot: %A" sexp)))
                                (match sexp with
                                    | List xs -> xs
                                    | Atom a -> [Atom a]
                                    | Quote q -> 
                                        raise (error (Symbol "parameter-error") (sprintf "Expected: Number\nGot: %A" sexp)))
            List.reduce (+) args |> Number |> Atom

let sub = fun (sexp) ->
            let args = List.map (fun (x) ->
                                    match x with
                                    |Atom (Number n) -> n
                                    |List ([Atom (Number n)]) -> n
                                    |any ->
                                        raise (error (Symbol "parameter-error") (sprintf "Expected: Number\nGot: %A" sexp)))
                                (match sexp with
                                    | List xs -> xs
                                    | Atom a -> [Atom a]
                                    | Quote q -> 
                                        raise (error (Symbol "parameter-error") (sprintf "Expected: Number\nGot: %A" sexp)))
            List.reduce (-) args |> Number |> Atom

let mul = fun (sexp) ->
            let args = List.map (fun (x) ->
                                    match x with
                                    |Atom (Number n) -> n
                                    |List ([Atom (Number n)]) -> n
                                    |any ->
                                        raise (error (Symbol "parameter-error") (sprintf "Expected: Number\nGot: %A" sexp)))
                                (match sexp with
                                    | List xs -> xs
                                    | Atom a -> [Atom a]
                                    | Quote q -> 
                                        raise (error (Symbol "parameter-error") (sprintf "Expected: Number\nGot: %A" sexp)))
            List.reduce (*) args |> Number |> Atom

let div = fun (sexp) ->
            let args = List.map (fun (x) ->
                                    match x with
                                    |Atom (Number n) -> n
                                    |List ([Atom (Number n)]) -> n
                                    |any ->
                                        raise (error (Symbol "parameter-error") (sprintf "Expected: Number\nGot: %A" sexp)))
                                (match sexp with
                                    | List xs -> xs
                                    | Atom a -> [Atom a]
                                    | Quote q -> 
                                        raise (error (Symbol "parameter-error") (sprintf "Expected: Number\nGot: %A" sexp)))
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
                                    raise (error (Symbol "parameter-error") (sprintf "Expected: Value\nGot: %A" sexp))
                let cdr_val = match cdr with
                                | Atom a -> a
                                | any ->
                                    raise (error (Symbol "parameter-error") (sprintf "Expected: Value\nGot: %A" sexp))
                Cons (car_val,cdr_val) |> Atom
            | any -> 
                raise (error (Symbol "parameter-error") (sprintf "Expected: 2 Values\nGot: %A" sexp))

let car = fun (sexp) ->
            match sexp with
            | List [Atom (Cons (carval,_))] -> Atom carval
            | any ->
                raise (error (Symbol "parameter-error") (sprintf "Expected: Cons\nGot: %A" sexp))

let cdr = fun (sexp) ->
            match sexp with
            | List [Atom (Cons (_,cdrval))] -> Atom cdrval
            | any ->
                raise (error (Symbol "parameter-error") (sprintf "Expected: Cons\nGot: %A" sexp))

let load (ctx:Context) =
    ctx.add (Symbol "add") (Function add)
    ctx.add (Symbol "sub") (Function sub)
    ctx.add (Symbol "mul") (Function mul)
    ctx.add (Symbol "div") (Function div)
    ctx.add (Symbol "equal") (Function equal)
    ctx.add (Symbol "cons") (Function cons)
    ctx.add (Symbol "car") (Function car)
    ctx.add (Symbol "cdr") (Function cdr)