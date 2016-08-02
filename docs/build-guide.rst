Build guide
===========

To use Naggum, first of all you need to build it from source.

Windows
-------

To build Naggum on Windows, just use Visual Studio or MSBuild like that::

    $ cd naggum
    $ nuget restore
    $ msbuild /p:Configuration=Release Naggum.sln

Linux
-----

See general build instructions for Linux in the file ``.travis.yml`` inside the
Naggum source directory.

You'll need `Mono`_, `NuGet`_ and `F# Compiler`_ installed. Some of them may or
may not be part of your Mono installation; just make sure you've got them all.

Below is an example of setting up these tools on `NixOS Linux`_; feel free to
add instructions for any other distributions.

NixOS Linux
^^^^^^^^^^^

*The instructions have been verified on NixOS 16.03. If something doesn't work, please file an issue.*

Enter the development environment::

    $ cd naggum
    $ nix-shell

After that you can download the dependencies and build the project using
``xbuild``::

    $ nuget restore
    $ xbuild /p:Configuration=Release /p:TargetFrameworkVersion="v4.5"

After that, you can run ``Naggum.Compiler``, for example::

    $ cd Naggum.Compiler/bin/Release/
    $ mono Naggum.Compiler.exe ../../../tests/test.naggum
    $ mono test.exe

Documentation
-------------

You can build a local copy of Naggum documentation. To do that, install
`Python`_ 2.7 and `Sphinx`_. Ensure that you have ``sphinx-build`` binary in
your ``PATH`` or define ``SPHINXBUILD`` environment variable to choose an
alternative Sphinx builder. After that go to `docs` directory and execute ``make
html`` (on Linux) or ``.\make.bat html`` (on Windows).

.. _F# Compiler: http://fsharp.org/
.. _Mono: http://www.mono-project.com/
.. _NixOS Linux: http://nixos.org/
.. _NuGet: http://www.nuget.org/
.. _Python: https://www.python.org/
.. _Sphinx: http://sphinx-doc.org/
