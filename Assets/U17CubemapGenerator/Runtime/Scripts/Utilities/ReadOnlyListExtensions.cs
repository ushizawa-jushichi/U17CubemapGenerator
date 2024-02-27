using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uchuhikoshi
{
    public static class ReadOnlyListExtensions
    {
		public static int IndexOf<T>(this IReadOnlyList<T> self, Func<T, bool> predicate)
		{
			int count = self.Count;
			for (int i = 0; i < count; i++)
			{
				if (predicate(self[i]))
					return i;
			}
			return -1;
		}
	}
}
