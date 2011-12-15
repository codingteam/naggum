module Naggum.Writer

type Writer =
    static member write (o : obj) : obj =
        let str = o.ToString()
        System.Console.Write str
        null

    static member writeln (o : obj) : obj =
        let str = o.ToString()
        System.Console.WriteLine str
        null