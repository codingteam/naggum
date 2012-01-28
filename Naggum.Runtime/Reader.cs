using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Naggum.Runtime
{
    class Reader
    {
        /// <summary>
        /// Checks if the character is constituent, i.e. not whitespace or list separator.
        /// </summary>
        /// <param name="c">character to be checked</param>
        /// <returns>true if the character is constituent, false otherwise</returns>
        public static bool isConstituent(char c)
        {
            return (!Char.IsWhiteSpace(c))
                && c != '('
                && c != ')';
        }

        /// <summary>
        /// Reads a symbol from a stream.
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <returns></returns>
        private static Object ReadSymbol(Stream stream)
        {
            bool in_symbol = true;
            StringBuilder symbol_name = new StringBuilder();
            StreamReader reader = new StreamReader(stream);
            while (in_symbol)
            {
                var ch = reader.Peek();
                if (ch < 0) throw new IOException("Unexpected end of stream.");
                if (isConstituent((char)ch))
                {
                    symbol_name.Append((char)reader.Read());
                }
                else
                {
                    in_symbol = false;
                }
            }
            if (symbol_name.Length > 0)
                return new Symbol(symbol_name.ToString());
            else
                throw new IOException("Empty symbol.");
        }

        /// <summary>
        /// Reads a list from input stream.
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <returns></returns>
        private static Object ReadList(Stream stream)
        {
            StreamReader reader = new StreamReader(stream);
            bool in_list = true;
            Cons list = null;
            while (in_list)
            {
                var ch = reader.Peek();
                if (ch < 0) throw new IOException("Unexpected end of stream.");
                if ((char)ch != ')')
                {
                    list = new Cons(Read(stream), list);
                }
                else
                {
                    in_list = false;
                }
            }
            return list;
        }

        /// <summary>
        /// Reads an object from input stream.
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <returns></returns>
        public static Object Read(Stream stream)
        {
            StreamReader reader = new StreamReader(stream);
            while (Char.IsWhiteSpace((char)reader.Peek())) reader.Read(); //consume all leading whitespace
            var ch = reader.Peek();
            if (ch < 0) return null;
            if (ch == '(') //beginning of a list
            {
                reader.Read(); //consume opening list delimiter.
                return ReadList(stream);
            }
            if (isConstituent((char)ch))
                return ReadSymbol(stream);

            throw new IOException("Unexpected char: " + (char)ch);
        }
    }
}
