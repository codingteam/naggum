using System;
using System.IO;
using Naggum.Runtime;

namespace Naggum.Interactive
{
    class Program
    {
        static void Main(string[] args)
        {
            Stream input = System.Console.OpenStandardInput();
            for (; ; )
            {
                System.Console.Out.Write(">");
                Object obj = Reader.Read(input);
                System.Console.Out.WriteLine(obj.ToString());
            }
            input.Close();
        }
    }
}
