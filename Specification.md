# Naggum Specification

## Features
+ based on CLR;
+ Lisp-2;
+ compiles to CIL assemblies;
+ is not a Common Lisp implementation;
+ seamlessly interoperates with other CLR code.

## Language
### Special forms
1. `(let (bindings*) body*)` where `bindings` follow a pattern of `(name initial-value)`.
`let` creates a lexical scope, evaluates initial values, binds them to corresponding names and evaluates the body, returning the value of last expression.
Naggum's `let` is a loner: every one is inherently iterative (like `let*`) and recursive (like `let rec`).

2. `(defun name (parms*) body*)`
`defun` defines a function (internally it will be a public static method). Naggum is a Lisp-2, henceforth a function and a variable can share their names.

3. `(if condition if-true [if-false])`
`if` evaluates given `condition`. If it is true (as in "not null, not zero, not false") it evaluates `if-true` form and returns it's result. If `condition` evaluates to false, null or zero then `if-false` form (if given) is evaluated and it's result (or null, if no `if-false` form is given) is returned from `if`.

4. `(fun-name args*)`
Applies function named `fun-name` to given arguments.

5. `(new type-name args*)`
`new` form calls applicable constructor of type named `type-name` with given arguments and returns created object.
 `(new (type-name generic-args*) args*)`
`new` form calls applicable constructor of generic type named `type-name`, assuming generic parameters in `generic-args` and with given arguments and returns created object.

6. `(call method-name object-var args*)`
Performs virtual call of method named `method-name` on object referenced by `object-var` with given arguments.

7. `(eval form [environment])`

8. `(error error-type args*)`
Throws an exception of `error-type`, constructed with `args`.

9. `(try form (catch-forms*))` where `catch-forms` follow a pattern of `(error-type handle-form)`
Tries to evaluate `form`. If any error is encountered, evaluates `handle-form` with the most appropriate `error-type`.

10. `(defmacro name (args*))`

11. `(require namespaces*)`

12. `(cond (cond-clauses*))`

13. `(set var-name value)`

### Quoting
1. `(quote form)`
2. `(quasi-quote form)`
3. `(unquote form)`
4. `(splice-unquote form)`

### Type declaration forms
* `(deftype type-name ([parent-types*]) members*)`
Defines CLR type, inheriting from `parent-types` with defined members.

* `(deftype (type-name generic-parms*) ([parent-types*]) members*)`
Defines generic CLR type, polymorphic by `generic-parms`, inheriting from `parent-types` with defined members.

* `(definterface type-name ([parent-types*]) members*)`
Defines CLR interface type, inheriting from `parent-types` with defined members.

* `(definterface (type-name generic-parms*) ([parent-types*]) members*)`
Defines generic CLR interface type, polymorphic by `generic-parms`, inheriting from `parent-types` with defined members.

If no `parent-types` is supplied, `System.Object` is assumed.

### Member declaration forms
* `(field [access-type] field-name)` -- declares a field with name given by `field-name` and access permissions defined by `access-type`.
* `(method [access-type] method-name (parms*) body*)` -- declares an instance method. Otherwise identical to `defun`.

Available values for `access-type` are `public`(available to everybody), `internal`(available to types that inherit from this type) and `private`(available only to methods in this type). If no `access-type` is given, `private` is assumed.

## Standard library
Naggum is designed to use CLR standard libraries, but some types and routines are provided to facilitate lisp-style programming.

### Cons
### Symbol
### Reader extensions
### Writer extensions