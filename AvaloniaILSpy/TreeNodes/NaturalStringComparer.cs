// Copyright (c) 2015 Siegfried Pammer
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace AvaloniaILSpy.TreeNodes
{
	public sealed class NaturalStringComparer : IComparer<string>
	{
		public static readonly NaturalStringComparer Instance = new NaturalStringComparer(CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);

		/// <summary>
		/// Create a sequence comparer, using the specified item comparer
		/// for T.
		/// </summary>
		public NaturalStringComparer(CultureInfo culture, CompareOptions options)
		{
			this.culture = culture;
			this.options = options;
		}

		/// <summary>
		/// culture used for comparing each element.
		/// </summary>
		CultureInfo culture;

		/// <summary>
		/// options used for comparing each element.
		/// </summary>
		CompareOptions options;

		/// <summary>
		/// Compare two sequences of T.
		/// </summary>
		/// <param name="x">First sequence.</param>
		/// <param name="y">Second sequence.</param>
		public int Compare(string x, string y)
		{
			CompareInfo cmp = culture.CompareInfo;
			int iA = 0;
			int iB = 0;
			int softResult = 0;
			int softResultWeight = 0;
			while (iA < x.Length && iB < y.Length)
			{
				bool isDigitA = Char.IsDigit(x[iA]);
				bool isDigitB = Char.IsDigit(y[iB]);
				if (isDigitA != isDigitB)
				{
					return cmp.Compare(x, iA, y, iB, options);
				}
				else if (!isDigitA && !isDigitB)
				{
					int jA = iA + 1;
					int jB = iB + 1;
					while (jA < x.Length && !Char.IsDigit(x[jA])) jA++;
					while (jB < y.Length && !Char.IsDigit(y[jB])) jB++;
					int cmpResult = cmp.Compare(x, iA, jA - iA, y, iB, jB - iB, options);
					if (cmpResult != 0)
					{
						// Certain strings may be considered different due to "soft" differences that are
						// ignored if more significant differences follow, e.g. a hyphen only affects the
						// comparison if no other differences follow
						string sectionA = x.Substring(iA, jA - iA);
						string sectionB = y.Substring(iB, jB - iB);
						if (cmp.Compare(sectionA + "1", sectionB + "2", options) ==
							cmp.Compare(sectionA + "2", sectionB + "1", options))
						{
							return cmp.Compare(x, iA, y, iB, options);
						}
						else if (softResultWeight < 1)
						{
							softResult = cmpResult;
							softResultWeight = 1;
						}
					}
					iA = jA;
					iB = jB;
				}
				else
				{
					char zeroA = (char)(x[iA] - (int)Char.GetNumericValue(x[iA]));
					char zeroB = (char)(y[iB] - (int)Char.GetNumericValue(y[iB]));
					int jA = iA;
					int jB = iB;
					while (jA < x.Length && x[jA] == zeroA) jA++;
					while (jB < y.Length && y[jB] == zeroB) jB++;
					int resultIfSameLength = 0;
					do
					{
						isDigitA = jA < x.Length && Char.IsDigit(x[jA]);
						isDigitB = jB < y.Length && Char.IsDigit(y[jB]);
						int numA = isDigitA ? (int)Char.GetNumericValue(x[jA]) : 0;
						int numB = isDigitB ? (int)Char.GetNumericValue(y[jB]) : 0;
						if (isDigitA && (char)(x[jA] - numA) != zeroA) isDigitA = false;
						if (isDigitB && (char)(y[jB] - numB) != zeroB) isDigitB = false;
						if (isDigitA && isDigitB)
						{
							if (numA != numB && resultIfSameLength == 0)
							{
								resultIfSameLength = numA < numB ? -1 : 1;
							}
							jA++;
							jB++;
						}
					}
					while (isDigitA && isDigitB);
					if (isDigitA != isDigitB)
					{
						// One number has more digits than the other (ignoring leading zeros) - the longer
						// number must be larger
						return isDigitA ? 1 : -1;
					}
					else if (resultIfSameLength != 0)
					{
						// Both numbers are the same length (ignoring leading zeros) and at least one of
						// the digits differed - the first difference determines the result
						return resultIfSameLength;
					}
					int lA = jA - iA;
					int lB = jB - iB;
					if (lA != lB)
					{
						// Both numbers are equivalent but one has more leading zeros
						return lA > lB ? -1 : 1;
					}
					else if (zeroA != zeroB && softResultWeight < 2)
					{
						softResult = cmp.Compare(x, iA, 1, y, iB, 1, options);
						softResultWeight = 2;
					}
					iA = jA;
					iB = jB;
				}
			}
			if (iA < x.Length || iB < y.Length)
			{
				return iA < x.Length ? 1 : -1;
			}
			else if (softResult != 0)
			{
				return softResult;
			}
			return 0;
		}
	}
}
