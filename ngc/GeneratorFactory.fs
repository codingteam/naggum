module Naggum.Compiler.GeneratorFactory

open IGenerator
open NumberGen
open StringGen
open FormGenerator
open ClrGenerator
open Context
open Naggum.Runtime
open Naggum.Util.MaybeMonad
open Naggum.Compiler.Reader
open Naggum.Compiler.MathGenerator
open System
open System.Reflection
open System.Reflection.Emit
open System.Text.RegularExpressions

type GeneratorFactory(typeBldr:TypeBuilder) =
    member private this.makeObjectGenerator(o:obj) =
        match o with
        | :? System.Int32 ->
            (new Int32Gen(o :?> System.Int32)) :> IGenerator
        | :? System.Int64 ->
            (new Int64Gen(o :?> System.Int64)) :> IGenerator
        | :? System.Single ->
            (new SingleGen(o :?> System.Single)) :> IGenerator
        | :? System.Double ->
            (new DoubleGen(o :?> System.Double)) :> IGenerator
        | :? System.String ->
            (new StringGen(o :?> System.String)) :> IGenerator
        | other -> failwithf "Not a basic value: %A\n" other

    member private this.makeValueGenerator (context: Context, value:Value) =
        match value with
        | Symbol name ->
            (new SymbolGenerator(context,name)) :> IGenerator
        | Object o -> this.makeObjectGenerator o

    member private this.MakeFormGenerator (context:Context, form:SExp list) =
        match form with
        | (Atom (Symbol "defun") :: Atom (Symbol name) :: List args :: body) ->
            new DefunGenerator(context,typeBldr,name,args,body,this) :> IGenerator
        | Atom (Symbol "if") :: condition :: if_true :: if_false :: [] -> //full if form
            new FullIfGenerator(context,typeBldr,condition,if_true,if_false,this) :> IGenerator
        | Atom (Symbol "if") :: condition :: if_true :: [] -> //reduced if form
            new ReducedIfGenerator(context,typeBldr,condition,if_true,this) :> IGenerator
        | Atom (Symbol "let") :: bindings :: body -> //let form
            new LetGenerator(context,typeBldr,bindings,body,this) :> IGenerator
        | Atom (Symbol "quote") :: quotedExp :: [] ->
            new QuoteGenerator(context,typeBldr,quotedExp,this) :> IGenerator
        | Atom (Symbol "new") :: Atom (Symbol typeName) :: args ->
            new NewObjGenerator(context,typeBldr,typeName,args,this) :> IGenerator
        | Atom (Symbol "+") :: args ->
            new ArithmeticGenerator(context,typeBldr,args,OpCodes.Add,this) :> IGenerator
        | Atom (Symbol "-") :: args ->
            new ArithmeticGenerator(context,typeBldr,args,OpCodes.Sub,this) :> IGenerator
        | Atom (Symbol "*") :: args ->
            new ArithmeticGenerator(context,typeBldr,args,OpCodes.Mul,this) :> IGenerator
        | Atom (Symbol "/") :: args ->
            new ArithmeticGenerator(context,typeBldr,args,OpCodes.Div,this) :> IGenerator
        | Atom (Symbol "=") :: arg_a :: arg_b :: [] ->
            new SimpleLogicGenerator(context,typeBldr,arg_a,arg_b,OpCodes.Ceq,this) :> IGenerator
        | Atom (Symbol "<") :: arg_a :: arg_b :: [] ->
            new SimpleLogicGenerator(context,typeBldr,arg_a,arg_b,OpCodes.Clt,this) :> IGenerator
        | Atom (Symbol ">") :: arg_a :: arg_b :: [] ->
            new SimpleLogicGenerator(context,typeBldr,arg_a,arg_b,OpCodes.Cgt,this) :> IGenerator
        |Atom (Symbol "call") :: Atom (Symbol fname) :: instance :: args ->
            new InstanceCallGenerator(context, typeBldr, instance, fname, args, this) :> IGenerator
        | Atom (Symbol fname) :: args -> //generic funcall pattern
            let tryGetType typeName =
                try Some (context.types.[new Symbol(typeName)]) with
                | _ ->
                    try Some (Type.GetType typeName) with
                    | _ -> None
                    
            
            let callRegex = new Regex(@"([\w\.]+)\.(\w+)", RegexOptions.Compiled)
            let callMatch = callRegex.Match fname
            let maybeClrType =
                maybe {
                    let! typeName = if callMatch.Success then Some callMatch.Groups.[1].Value else None
                    let! clrType = tryGetType typeName
                    return clrType
                }

            if Option.isSome maybeClrType then
                let clrType = Option.get maybeClrType
                let methodName = callMatch.Groups.[2].Value
                new ClrCallGenerator(context, typeBldr, clrType, methodName, args, this) :> IGenerator
            else
                new FunCallGenerator(context,typeBldr,fname,args,this) :> IGenerator
        | _ -> failwithf "Form %A is not supported yet" list

    member private this.makeSequenceGenerator(context: Context,seq:SExp list) =
        new SequenceGenerator(context,typeBldr,seq,(this :> IGeneratorFactory))

    member private this.makeBodyGenerator(context: Context,body:SExp list) =
        new BodyGenerator(context,typeBldr,body,(this :> IGeneratorFactory))

    interface IGeneratorFactory with
        member this.MakeGenerator context sexp =
            match sexp with
            | Atom value -> this.makeValueGenerator (context, value)
            | List form -> this.MakeFormGenerator (context,form)

        member this.MakeSequence context seq = this.makeSequenceGenerator (context,seq) :> IGenerator

        member this.MakeBody context body = this.makeBodyGenerator (context,body) :> IGenerator

        member this.MakeGeneratorFactory newTypeBuilder = (new GeneratorFactory (newTypeBuilder)) :> IGeneratorFactory
