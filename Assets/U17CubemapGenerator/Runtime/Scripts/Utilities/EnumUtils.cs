using System;
using System.Collections.Generic;

#nullable enable

namespace Uchuhikoshi
{
	public static class EnumUtils
	{
		public static List<T> ListOfEnum<T>() where T : struct
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("T must be an enumerated type");
			}
			var texts = new List<T>();
			foreach (T enumValue in Enum.GetValues(typeof(T)))
			{
				texts.Add(enumValue);
			}
			return texts;
		}

		public static List<string> StringListOfEnum<T>(IReadOnlyList<T> list) where T : struct
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("T must be an enumerated type");
			}

			var texts = new List<string>();
			foreach (T enumValue in list)
			{
				texts.Add(enumValue.ToString());
			}
			return texts;
		}

		public static List<string> StringListOfEnum<T>() where T : struct
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("T must be an enumerated type");
			}

			var texts = new List<string>();
			foreach (T enumValue in Enum.GetValues(typeof(T)))
			{
				texts.Add(enumValue.ToString());
			}
			return texts;
		}

		public static int IndexOfEnum<T>(T value, int defaultValue = -1) where T : struct, IComparable
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("T must be an enumerated type");
			}

			int index = 0;
			foreach (T enumValue in Enum.GetValues(typeof(T)))
			{
				if (enumValue.Equals(value))
				{
					return index;
				}
				index++;
			}
			return defaultValue;
		}

		public static T EnumByIndex<T>(int index, T defaultValue = default) where T : struct
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("T must be an enumerated type");
			}

			var ary = Enum.GetValues(typeof(T));
			if (index >= 0 && index < ary.Length)
			{
				return (T)ary.GetValue(index);
			}
			return defaultValue;
		}

		public static bool ParseEnum<T>(string value, out T ret) where T : struct, IConvertible
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("T must be an enumerated type");
			}
			if (string.IsNullOrEmpty(value))
			{
				ret = default(T);
				return false;
			}

			foreach (T item in Enum.GetValues(typeof(T)))
			{
				if (item.ToString().ToLower().Equals(value.Trim().ToLower()))
				{
					ret = item;
					return true;
				}
			}
			ret = default(T);
			return false;
		}
	}
}
