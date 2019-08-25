Naggum usage
============

Currently there are two dialects of Naggum: high-level *Compiler* and low-level
*Assembler*.

Naggum Compiler
---------------

Command line syntax for Naggum Compiler is::

    $ Naggum.Compiler source.naggum... [/r:assembly]...

Each input source file will be compiled to a separate executable assembly (i.e.
an ``.exe`` file) in the current directory. You can also pass a list of files to
be referenced by these assemblies.

``.naggum`` extension is recommended for high-level Naggum files.

Naggum Assembler
----------------

Naggum Assembler uses low-level Naggum dialect. Command line syntax is::

    $ Naggum.Assembler source.nga...

Each input file may contain zero or more assembly constructs. Every assembly
will be saved to its own executable file in the current directory.

``.nga`` extension is recommended for low-level Naggum files.

S-expression syntax
-------------------

Each Naggum program (either high-level or low-level) is written as a sequence of
S-expression forms. In s-expression, everything is either an atom or a list.
Atoms are written as-is, lists should be taken into parens.

Possible atom values are::

    "A string"
    1.4e-5 ; a number
    System.Console ; a symbol

A symbol is a sequence of letters, digits, and any of the following characters:
``+-*/=<>!?.``.

Lists are simply sequences of s-expressions in parens::

    (this is a list)

    (this (is ("Also") a.list))

Naggum source code may also include comments. Everything after ``;`` character
will be ignored till the end of the line::

    (valid atom) ; this is a comment

Low-level syntax
----------------

Naggum low-level syntax is closer to `CIL`_. It may be used to define CLI
constructs such as assemblies, modules, types and methods. Every ``.nga`` file
may contain zero or more assembly definitions.

Assembly definition
^^^^^^^^^^^^^^^^^^^

Assembly defitinion should have the following form::

    (.assembly Name
        Item1
        Item2
        ...)

Assembly items can be methods and types. Top level methods defined in an
``.assembly`` form will be compiled to global CIL functions.

Type definitions are not supported yet.

Each assembly may contain one entry point method (either a static type method or
an assembly global function marked by ``.entrypoint`` property).

Method definition
^^^^^^^^^^^^^^^^^

Method definition should have the following form::

    (.method Name (argument types) return-type (metadata items)
        body-statements
        ...)

Method argument and return types should be fully-qualified (e.g. must include a
namespace: for example, ``System.Void``).

The only supported metadata item is ``.entrypoint``. It marks a method as an
assembly entry point.

Method example::

    (.method Main () System.Void (.entrypoint)
        (ldstr "Hello, world!")
        (call (mscorlib System.Console WriteLine (System.String) System.Void))
        (ret))

Method body should be a CIL instruction sequence.

CIL instructions
^^^^^^^^^^^^^^^^

Currently only a small subset of all available CIL instructions is supported by
Naggum. This set will be extended in future.

#. Call instruction::

    (call (assembly type-name method-name (argument types) return-type))

   Currently assembly name is ignored; only ``mscorlib`` methods can be called.
   Static assembly function calls are not supported yet.

   Method argument and return types should be fully-qualified.

#. Load string instruction::

    (ldstr "Hello, world")

   Loads a string onto a CLI stack.

#. Return instruction::

    (ret)

   Return from current method.

Example assembly definition
^^^^^^^^^^^^^^^^^^^^^^^^^^^

::

    (.assembly Hello
        (.method Main () System.Void (.entrypoint)
            (ldstr "Hello, world!")
            (call (mscorlib System.Console WriteLine (System.String) System.Void))
            (ret)))

High-level syntax
-----------------

Every high-level Naggum program is a sequence of function definitions and a
top-level executable statements. Functions defined in an assembly are also
available as public static methods to be called by external assemblies.

Functions are defined using ``defun`` special form::

    (defun function-name (arg1 arg2)
        statement1
        statement2)

For example::

    (defun println (arg)
        (System.Console.WriteLine arg))

Naggum is a Lisp-2, henceforth a function and a variable can share their names.

Currently executable statements may be one of the following.

#. Let bindings::

    (let ((variable-name expression)
          (variable-name-2 expression-2))
         body
         statements)

Creates a lexical scope, evaluates initial values, binds them to corresponding
names and evaluates the body, returning the value of last expression.

Naggum's ``let`` is a loner: every one is inherently iterative (like ``let*``)
and recursive (like `let rec`).

#. Arithmetic statements::

    (+ 2 2)

#. Function calls::

    (defun func () (+ 2 2))

    (func)

#. Static CLI method calls::

    (System.Console.WriteLine "Math:")

#. Conditional statements::

    (if condition
        true-statement
        false-statement)

If the ``condition`` is true (as in "not null, not zero, not false") it
evaluates the ``true-statement`` form and returns its result. If the
``condition`` evaluates to false, null or zero, then the ``false-statement``
form is evaluated and its result is returned from ``if``.

#. Reduced if statements::

    (if condition
        true-statement)

#. Constructor calls::

    (new Naggum.Runtime.Cons "OK" "FAILURE")

Calls an applicable constructor of a type named `Naggum.Runtime.Cons` with the
given arguments and returns an object created.

.. _CIL: https://en.wikipedia.org/wiki/Common_Intermediate_Language
