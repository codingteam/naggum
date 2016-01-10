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

First of all, install ``mono``, ``fsharp`` and ``dotnetPackages.Nuget``::

    $ nix-env -i mono fsharp
    $ nix-env -iA nixos.pkgs.dotnetPackages.Nuget

Please note that you need ``fsharp`` >= 4.0 to build Naggum, which is currently
in the unstable channel. So maybe you'll need to switch to the unstable channel
or build ``fsharp`` from `Nixpkgs`_ source.

According to the recommendations in `patch-fsharp-targets`_ build helper,
you may want to set up the environment variable ``FSharpTargetsPath`` either
globally or locally::

    $ export FSharpTargetsPath=$(dirname $(which fsharpc))/../lib/mono/4.5/Microsoft.FSharp.Targets

After that you can download the dependencies and build the project using
``xbuild``::

    $ cd naggum
    $ nuget restore
    $ xbuild /p:Configuration=Release /p:TargetFrameworkVersion="v4.5"

After that you should copy ``FSharp.Core.dll`` to the project output directory,
because currently Nix have problems with concept of Mono global assembly cache::

    $ FSHARP_CORE=$HOME/.nix-profile/lib/mono/Reference\ Assemblies/Microsoft/FSharp/.NETFramework/v4.0/4.4.0.0/FSharp.Core.dll
    $ echo Naggum.*/bin/Release | xargs -n 1 cp -f "$FSHARP_CORE"

After that, you can run ``Naggum.Compiler``, for example::

    $ cd Naggum.Compiler/bin/Release/
    $ mono Naggum.Compiler.exe ../../../tests/test.naggum
    $ mono test.exe

Documentation
-------------

You can build a local copy of Naggum documentation. To do that, install
`Python`_ 2.7 and `Sphinx`_. After that go to `docs` directory and execute
``make html`` on Linux or ``.\make.bat html`` on Windows.

Ensure you have ``sphinx-build`` binary in your ``PATH`` or define
``SPHINXBUILD`` environment variable to choose an alternative Sphinx builder.

.. _F# Compiler: http://fsharp.org/
.. _Mono: http://www.mono-project.com/
.. _NixOS Linux: http://nixos.org/
.. _Nixpkgs: https://github.com/NixOS/nixpkgs
.. _NuGet: http://www.nuget.org/
.. _patch-fsharp-targets:  https://github.com/NixOS/nixpkgs/blob/d4681bf62672083f92545e02e00b8cf040247e8d/pkgs/build-support/dotnetbuildhelpers/patch-fsharp-targets.sh
.. _Python: https://www.python.org/
.. _Sphinx: http://sphinx-doc.org/
