﻿using System;
using System.Text;
using System.Collections;

namespace Naggum.Runtime
{
    /// <summary>
    /// Cons-cell and basic cons manipulation functions.
    /// </summary>
    public class Cons : IEquatable<Cons>
    {
        public Object pCar { get; set; }
        public Object pCdr { get; set; }

        /// <summary>
        /// The real cons-cell constructor.
        /// </summary>
        /// <param name="aCar">CAR part of the new cell</param>
        /// <param name="aCdr">CDR part of the new cell </param>
        public Cons(Object aCar, Object aCdr)
        {
            pCar = aCar;
            pCdr = aCdr;
        }

        /// <summary>
        /// </summary>
        /// <param name="aCons">Cons-cell</param>
        /// <returns>CAR part of given cell</returns>
        public static Object Car(Cons aCons)
        {
            return aCons.pCar;
        }
        /// <summary>
        /// </summary>
        /// <param name="aCons">Cons-cell</param>
        /// <returns>CDR part of given cell</returns>
        public static Object Cdr(Cons aCons)
        {
            return aCons.pCdr;
        }

        /// <summary>
        /// Checks if the cons-cell is a list
        /// </summary>
        /// <param name="aCons">Cons-cell</param>
        /// <returns>True if CDR part of the cell is a list or is null.
        /// False otherwise.</returns>
        public static bool IsList(Cons aCons)
        {
            if (aCons == null) return true; //Empty list is still a list.
            if (aCons.pCdr == null) return true; //List with one element is a list;
            else if (aCons.pCdr.GetType() == typeof(Cons)) return IsList((Cons)aCons.pCdr);
            else return false; //If it's not null or not a list head, then it's definitely not a list.
        }

        /// <summary>
        /// Converts cons-cell to string representation.
        /// </summary>
        /// <returns>String representation of cons-cell.</returns>
        public override String ToString()
        {
            StringBuilder buffer = new StringBuilder("");
            buffer.Append("(");
            if (IsList(this))
            {
                for (Cons it = this; it != null; it = (Cons)it.pCdr)
                {
                    buffer.Append(it.pCar.ToString());
                    if (it.pCdr != null) buffer.Append(" ");
                }
            }
            else
            {
                buffer.Append(pCar.ToString()).Append(" . ").Append(pCdr.ToString());
            }
            buffer.Append(")");
            return buffer.ToString();
        }

        /// <summary>
        /// Checks cons cell for equality with other cell.
        /// </summary>
        /// <param name="other">Other cons cell</param>
        /// <returns>True if other cell is equal to this; false otherwise.</returns>
        bool IEquatable<Cons>.Equals(Cons other)
        {
            return pCar == other.pCar && pCdr == other.pCdr;
        }

        /// <summary>
        /// Constructs a list.
        /// </summary>
        /// <param name="elements">Elements of a list.</param>
        /// <returns>List with given elements.</returns>
        public static Cons List(params object[] elements)
        {
            Cons list = null;
            Array.Reverse(elements);
            foreach (var element in elements)
            {
                var tmp = new Cons(element, list);
                list = tmp;
            }
            return list;
        }
    }
}
