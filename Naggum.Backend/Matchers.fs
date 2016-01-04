module Naggum.Backend.Matchers

let (|String|Symbol|Object|List|) = function
    | Reader.Atom (Reader.Object (:? string as s)) -> String s
    | Reader.Atom (Reader.Object o) -> Object o
    | Reader.Atom (Reader.Symbol x) -> Symbol x
    | Reader.List l -> List l
