Naggum usage
============

Currently there are two dialects of Naggum: high level *Compiler* and low-level
*Assembler*.

Naggum Compiler
---------------

Command line syntax for Naggum Compiler is::

    $ Naggum.Compiler [source1.naggum] [source2.naggum] [/r:assembly1] [/r:assembly2]

Each input source file will be compiled to a separate executable assembly (i.e.
an ``.exe`` file) in the current directory.

``.naggum`` extension is recommended for high level Naggum files.

Naggum Assembler
----------------

Naggum Assembler uses low level Naggum dialect. Command line syntax is::

    $ Naggum.Assembler [source1.nga] [source2.nga]

Assembler output is determined based on input file content: each file may
contain multiple (or none) assembly constructs. Each assembly will be saved to
its own executable file in the current directory.

``.nga`` extension is recommended for low level Naggum files.

S-expression syntax
-------------------

Each Naggum program (either high-level or low-level) is written as a sequence of
S-expression forms. In s-expression, everything is either an atom or a list.
Atoms are written as-is, lists should be taken into parens.

Possible atom values are::

    "A string"
    1.4e-5 ; a number
    System.Console ; a symbol

A symbol may consist of any letter, digit or ``+-*/=<>!?.`` character sequence.

Lists are simply sequences of s-expression in parens::

    (this is a list)

    (this (is ("Also") a.list))

Naggum source code may also include comments. Everything after ``;`` character
will be ignored till the end of the line::

    (valid atom) ; this is a comment

Low level syntax
----------------

Naggum low level syntax is closer to `CIL`_. It may be used to define CLI
constructs such as assemblies, modules, types and methods. Every ``.nga`` file
may contain multiple (including zero) assembly definitions.

Assembly definition
^^^^^^^^^^^^^^^^^^^

Assembly defitinion should have the following form::

    (.assembly Name
        Item1
        Item2
        ...)

Assembly items can be methods and types. Top level methods defined in an
``.assembly`` form will be compiled to global CIL functions.

Currently there is no support for type definition inside an assembly.

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
   Static assembly function calls are not supported, but will be supported in
   future.

   Method argument and return types should be fully-qualified.

#. Load string instruction::

    (ldstr "Hello, world")

   Loads a string to CLI stack.

#. Return instruction::

    (ret)

   Return from current method.

Example assembly definition
^^^^^^^^^^^^^^^^^^^^^^^^^^^

::

    (.method Main () System.Void (.entrypoint)
        (ldstr "Hello, world!")
        (call (mscorlib System.Console WriteLine (System.String) System.Void))
        (ret)))

High level syntax
-----------------

Every high level Naggum program is a sequence of function definitions or a
top-level executable statements. Functions may be called from top-level
statements and will be available as a public static methods outside of an
compiled assembly.

Functions are defined using ``defun`` special form::

    (defun function-name (arg1 arg2)
        statement1
        statement2)

Currently executable statements may be one of the following.

#. Arithmetic statements::

    (+ 2 2)

#. Function calls::

    (defun func () (+ 2 2))

    (func)

#. Static CLI method calls::

    (System.Console.WriteLine "Math:")

#. If statements::

    (if condition
        true-statement
        false-statement)

#. Reduced if statements::

    (if condition
        true-statement)

#. Let bindings::

    (let ((variable-name expression)
          (variable-name-2 expression-2))
         body
         statements)

#. Constructor calls::

    (new Naggum.Runtime.Cons "OK" "FAILURE")

.. _CIL: https://en.wikipedia.org/wiki/Common_Intermediate_Language
