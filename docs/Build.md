Naggum Build Guide
==================

Windows
-------

To build Naggum on Windows, just use Visual Studio or MSBuild like that:

    $ cd naggum
    $ nuget restore
    $ msbuild /p:Configuration=Release Naggum.sln

Linux
-----

See general build instructions for Linux in the file `.travis.yml` inside the
Naggum source directory.

You'll need [Mono][mono], [NuGet][nuget] and [F# Compiler][fsharp] installed.
Some of them may or may not be part of your Mono installation; just make sure
you've got them all.

Below is an example of setting up these tools on [NixOS Linux][nixos]; feel free
to add instructions for any other distributions.

### NixOS Linux

First of all, install `mono` and `fsharp` packages to your environment:

    $ nix-env -i mono fsharp

Please note that you need `fsharp` >= 4.0 to build Naggum, which is currently in
the unstable channel. So maybe you'll need to switch to the unstable channel or
build `fsharp` from [Nixpkgs][nixpkgs] source.

According to the recommendations in
[`patch-fsharp-targets`][patch-fsharp-targets] build helper, you may want to set
up the environment variable `FSharpTargetsPath` either globally or locally:

    $ export FSharpTargetsPath=$(dirname $(which fsharpc))/../lib/mono/4.5/Microsoft.FSharp.Targets

You'll need a NuGet executable. Unfortunately, there is no NixOS package for
NuGet, so you have to download it manually:

    $ nix-env -i wget # if not installed already
    $ cd naggum
    $ wget https://dist.nuget.org/win-x86-commandline/latest/nuget.exe

After that you can download the dependencies and build the project using
`xbuild`:

    $ mono nuget.exe restore Naggum.sln
    $ xbuild /p:Configuration=Release /p:TargetFrameworkVersion="v4.5" Naggum.sln

After that you should copy `FSharp.Core.dll` to the project output directory,
because currently Nix have problems with concept of Mono global assembly cache:

    $ FSHARP_CORE=$HOME/.nix-profile/lib/mono/Reference\ Assemblies/Microsoft/FSharp/.NETFramework/v4.0/4.4.0.0/FSharp.Core.dll
    $ echo Naggum.*/bin/Release | xargs -n 1 cp -f "$FSHARP_CORE"

After that, you can run Naggum.Compiler, for example:

    $ cd Naggum.Compiler/bin/Release/
    $ mono Naggum.Compiler.exe ../../../tests/test.naggum
    $ mono test.exe

[fsharp]: http://fsharp.org/
[mono]: http://www.mono-project.com/
[nixos]: http://nixos.org/
[nixpkgs]: https://github.com/NixOS/nixpkgs
[nuget]: http://www.nuget.org/
[patch-fsharp-targets]: https://github.com/NixOS/nixpkgs/blob/d4681bf62672083f92545e02e00b8cf040247e8d/pkgs/build-support/dotnetbuildhelpers/patch-fsharp-targets.sh
