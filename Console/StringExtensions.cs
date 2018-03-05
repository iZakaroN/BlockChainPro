using System;
using System.Collections.Generic;

namespace BlockChanPro.Console
{
	public static class StringExtensions
	{
		public static IEnumerable<string> Split(this string str, Func<char, bool> canSeparate)
		{
			var nextPiece = 0;

			for (var c = 0; c < str.Length; c++)
			{
				if (canSeparate(str[c]))
				{
					yield return str.Substring(nextPiece, c - nextPiece);
					nextPiece = c + 1;
				}
			}

			yield return str.Substring(nextPiece);
		}
	}

}
