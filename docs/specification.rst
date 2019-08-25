Naggum Specification
====================

Features
--------

-  based on CLR;
-  Lisp-2;
-  compiles to CIL assemblies;
-  is not a Common Lisp implementation;
-  seamlessly interoperates with other CLR code.

Language
--------

Special forms
~~~~~~~~~~~~~

1.  ``(let (bindings*) body*)`` where ``bindings`` follow a pattern of
    ``(name initial-value)`` creates a lexical scope, evaluates initial
    values, binds them to corresponding names and evaluates the body,
    returning the value of last expression. Naggum’s ``let`` is a loner:
    every one is inherently iterative (like ``let*``) and recursive
    (like ``let rec``).

2.  ``(defun name (parms*) body*)`` defines a function (internally it
    will be a public static method). Naggum is a Lisp-2, henceforth a
    function and a variable can share their names.

3.  ``(if condition if-true [if-false])`` evaluates given ``condition``.
    If it is true (as in “not null, not zero, not false”) it evaluates
    ``if-true`` form and returns it’s result. If ``condition`` evaluates
    to false, null or zero then ``if-false`` form (if given) is
    evaluated and it’s result (or null, if no ``if-false`` form is
    given) is returned from ``if``.

4.  ``(fun-name args*)`` applies function named ``fun-name`` to given
    arguments.

5.  ``(new type-name args*)`` calls applicable constructor of type named
    ``type-name`` with given arguments and returns created object.
    ``(new (type-name generic-args*) args*)`` ``new`` form calls
    applicable constructor of generic type named ``type-name``, assuming
    generic parameters in ``generic-args`` and with given arguments and
    returns created object.

6.  ``(call method-name object-var args*)`` Performs virtual call of
    method named ``method-name`` on object referenced by ``object-var``
    with given arguments.

7.  ``(lambda (parms*) body*)`` Constructs anonymous function with
    ``parms`` as parameters and ``body`` as body and returns it as a
    result.

8.  ``(eval form [environment])`` evaluates form using supplied lexical
    environment. If no environment is given, uses current one.

9.  ``(error error-type args*)`` throws an exception of ``error-type``,
    constructed with ``args``.

10. ``(try form (catch-forms*))`` where ``catch-forms`` follow a pattern
    of ``(error-type handle-form)`` tries to evaluate ``form``. If any
    error is encountered, evaluates ``handle-form`` with the most
    appropriate ``error-type``.

11. ``(defmacro name (args*))`` defines a macro that will be expanded at
    compile time.

12. ``(require namespaces*)`` states that ``namespaces`` should be used
    to search for symbols.

13. ``(cond (cond-clauses*))`` where ``cond-clauses`` follow a pattern
    of ``(condition form)`` sequentially evaluates conditions, until one
    of them is evaluated to ``true``, non-null or non-zero value, then
    the corresponding ``form`` is evaluated and it’s result returned.

14. ``(set var value)`` sets the value of ``var`` to ``value``. ``var``
    can be a local variable, function parameter or a field of some
    object.

Quoting
~~~~~~~

1. ``(quote form)`` indicates simple quoting. ``form`` is returned
   as-is.

2. ``(quasi-quote form)`` returns ``form`` with ``unquote`` and
   ``splice-unquote`` expressions inside evaluated and substituted with
   their results accordingly

3. ``(unquote form)`` if encountered in ``quasi-quote`` form, will be
   substituted by a result of ``form`` evaluation

4. ``(splice-unquote form)`` same as ``unquote``, but if ``form``
   evaluation result is a list, then it’s elements will be spliced as an
   elements of the containing ``list``.

Type declaration forms
~~~~~~~~~~~~~~~~~~~~~~

-  ``(deftype type-name ([parent-types*]) members*)`` Defines CLR type,
   inheriting from ``parent-types`` with defined members.

-  ``(deftype (type-name generic-parms*) ([parent-types*]) members*)``
   Defines generic CLR type, polymorphic by ``generic-parms``,
   inheriting from ``parent-types`` with defined members.

-  ``(definterface type-name ([parent-types*]) members*)`` Defines CLR
   interface type, inheriting from ``parent-types`` with defined
   members.

-  ``(definterface (type-name generic-parms*) ([parent-types*]) members*)``
   Defines generic CLR interface type, polymorphic by ``generic-parms``,
   inheriting from ``parent-types`` with defined members.

If no ``parent-types`` is supplied, ``System.Object`` is assumed.

Member declaration forms
~~~~~~~~~~~~~~~~~~~~~~~~

-  ``(field [access-type] field-name)`` declares a field with name given
   by ``field-name`` and access permissions defined by ``access-type``.
-  ``(method [access-type] method-name (parms*) body*)`` declares an
   instance method. Otherwise identical to ``defun``.

Available values for ``access-type`` are ``public``\ (available to
everybody), ``internal``\ (available to types that inherit from this
type) and ``private``\ (available only to methods in this type). If no
``access-type`` is given, ``private`` is assumed.

Standard library
----------------

Naggum is designed to use CLR standard libraries, but some types and
routines are provided to facilitate lisp-style programming.

Cons
~~~~

Cons-cell is the most basic building block of complex data structures.
It contains exactly two objects of any types, referenced as *CAR* (left
part, head) and *CDR* (right part, tail)

Symbol
~~~~~~

Symbol is a type that represents language primitives like variable,
function and type names.

Naggum Reader
~~~~~~~~~~~~~

Reader reads Lisp objects from any input stream, returning them as lists
and atoms.

Naggum Writer
~~~~~~~~~~~~~

Writer writes Lisp objects to any output stream, performing output
formatting if needed.
