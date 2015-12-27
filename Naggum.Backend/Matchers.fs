module Naggum.Backend.Matchers

let (|Symbol|Object|List|) = function
    | Reader.Atom (Reader.Object o) -> Object o
    | Reader.Atom (Reader.Symbol x) -> Symbol x
    | Reader.List l -> List l

let (|Integer|_|) = function
    | Object (:? int as i) -> Some i
    | _ -> None

let (|String|_|) = function
    | Object (:? string as s) -> Some s
    | _ -> None
