module Naggum.Types.Cons

type Cons(carval,cdrval) =
    member public this.car = carval
    member public this.cdr = cdrval
    static member Cons(carval:obj,cdrval:obj) =
        new Cons(carval,cdrval) :> obj
    static member Car (cons:obj) =
        match cons with
        | :? Cons -> (cons :?> Cons).car
        | _ -> failwithf "Not a cons: %A" cons
    static member Cdr (cons:obj) =
        match cons with
        | :? Cons -> (cons :?> Cons).cdr
        | _ -> failwithf "Not a cons: %A" cons
