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
