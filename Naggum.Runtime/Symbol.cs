/*  Copyright (C) 2012 by ForNeVeR, Hagane

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

using System;

namespace Naggum.Runtime
{
    public class Symbol : IEquatable<Symbol>
    {
        public String Name { get; set; }
        /// <summary>
        /// Constructs new symbol object.
        /// </summary>
        /// <param name="aName">Symbol name</param>
        public Symbol(String aName)
        {
            Name = aName;
        }

        bool IEquatable<Symbol>.Equals(Symbol other)
        {
			return AreEqual(this, other);
        }

		public override bool Equals(object obj)
		{
			var symbol = obj as Symbol;
			if (symbol != null)
			{
				return AreEqual(this, symbol);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

        /// <summary>
        /// </summary>
        /// <returns>Returns symbol's name as string.</returns>
        public override string ToString()
        {
            return Name;
        }

		private static bool AreEqual(Symbol one, Symbol other)
		{
			return one.Name.Equals(other.Name);
		}
    }
}
