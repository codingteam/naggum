**Naggum** compiler can be invoked with the command
`ngc [<source-files>] [options]` where `<options>` can be one or more of the following:

* `/r:<filename>` -- instructs the compiler to load external assembly from `<filename>`;
* `/o:<filename>` -- instructs the compiler to write produced assembly to `<filename>`;
* `/t:<target>` -- defines the compilation target, that can be either `exe`(default) to produce an executable or `dll` to produce a DLL assembly.