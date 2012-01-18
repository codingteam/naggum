/*  Copyright (C) 2011 by ForNeVeR,Hagane

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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Naggum.Runtime
{
    /// <summary>
    /// Cons-cell and basic cons manipulation functions.
    /// </summary>
    public class Cons
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
            if (aCons.pCdr == null) return true; //Empty list is still a list.
            else if (aCons.pCdr.GetType() == typeof(Cons)) return IsList((Cons)aCons.pCdr);
            else return false; //If it's not null or not a list head, then it's definitely not a list.
        }

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
    }
}
