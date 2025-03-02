using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#nullable enable

namespace Uchuhikoshi
{
	public static class EnumUtility
	{
		public static List<T> ListOfEnum<T>() where T : struct
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("T must be an enumerated type");
			}
			return new List<T>((T[])Enum.GetValues(typeof(T)));
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

		public static bool ParseEnum<T>(string value, out T ret) where T : Enum, IConvertible
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("T must be an enumerated type");
			}
			if (string.IsNullOrEmpty(value))
			{
				ret = default(T)!;
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
			ret = default(T)!;
			return false;
		}

		// UnityのInspectorName属性が付けられていればその名前でリストにする
		public static List<string> ToInspectorNames(Type t)
		{
			List<string> ret = new List<string>();
			foreach (MemberInfo mi in t.GetMembers( BindingFlags.Static | BindingFlags.Public))
			{
				InspectorNameAttribute inspectorNameAttribute = (InspectorNameAttribute)Attribute.GetCustomAttribute(mi, typeof(InspectorNameAttribute));
				if (null == inspectorNameAttribute)
				{
					ret.Add(mi.Name);
					continue;
				}
			
				ret.Add(inspectorNameAttribute.displayName);			
			}
			return ret;
		}

	}
}
