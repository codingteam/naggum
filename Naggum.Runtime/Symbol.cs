using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            return Name.Equals(other.Name);
        }
    }
}
