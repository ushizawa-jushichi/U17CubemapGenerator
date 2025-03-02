using System;
using UnityEngine;

#nullable enable

namespace Uchuhikoshi
{
	public static class KeyValueStore
	{
		public static void SaveInt(string key, int value)
		{
			PlayerPrefs.SetInt(key, value);
		}

		public static void SaveFloat(string key, float value)
		{
			PlayerPrefs.SetFloat(key, value);
		}

		public static void SaveBool(string key, bool value)
		{
			PlayerPrefs.SetInt(key, value ? 1 : 0);
		}

		public static void SaveString(string key, string value)
		{
			PlayerPrefs.SetString(key, value);
		}

		public static void SaveVector2(string key, Vector2 value)
		{
			PlayerPrefs.SetString(key, $"{value.x},{value.y}");
		}

		public static void SaveVector2Int(string key, Vector2Int value)
		{
			PlayerPrefs.SetString(key, $"{value.x},{value.y}");
		}

		public static void SaveVector3(string key, Vector3 value)
		{
			PlayerPrefs.SetString(key, $"{value.x},{value.y},{value.z}");
		}

		public static void SaveVector3Int(string key, Vector3Int value)
		{
			PlayerPrefs.SetString(key, $"{value.x},{value.y},{value.z}");
		}

		public static void SaveVector4(string key, Vector4 value)
		{
			PlayerPrefs.SetString(key, $"{value.x},{value.y},{value.z},{value.w}");
		}

		public static int LoadInt(string key, int defaultValue)
		{
			return PlayerPrefs.GetInt(key, defaultValue);
		}

		public static float LoadFloat(string key, float defaultValue)
		{
			return PlayerPrefs.GetFloat(key, defaultValue);
		}

		public static bool LoadBool(string key, bool defaultValue)
		{
			return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;
		}

		public static string LoadString(string key, string defaultValue)
		{
			return PlayerPrefs.GetString(key, defaultValue);
		}

		static void LoadVectorAndSplit<T>(ref T x, string key, Func<string[], T, T> onSplit)
		{
			string str = LoadString(key, string.Empty);
			if (!string.IsNullOrEmpty(str))
			{
				string[] strs = str.Split(',');
				x = onSplit.Invoke(strs, x);
			}
		}

		public static Vector2 LoadVector2(string key, Vector2 defaultValue)
		{
			Vector2 tmp = defaultValue;
			LoadVectorAndSplit<Vector2>(ref tmp, key, onSplit: (strs, tmp) =>
			{
				float.TryParse(strs[0], out tmp.x);
				float.TryParse(strs[1], out tmp.y);
				return tmp;
			});
			return tmp;
		}

		public static Vector2Int LoadVector2Int(string key, Vector2Int defaultValue)
		{
			Vector2Int tmp = defaultValue;
			LoadVectorAndSplit<Vector2Int>(ref tmp, key, onSplit: (strs, tmp) =>
			{
				int tmpx = tmp.x;
				int tmpy = tmp.y;
				int.TryParse(strs[0], out tmpx);
				int.TryParse(strs[1], out tmpy);
				return new Vector2Int(tmpx, tmpy);
			});
			return tmp;
		}

		public static Vector3 LoadVector3(string key, Vector3 defaultValue)
		{
			Vector3 tmp = defaultValue;
			LoadVectorAndSplit<Vector3>(ref tmp, key, onSplit: (strs, tmp) =>
			{
				float.TryParse(strs[0], out tmp.x);
				float.TryParse(strs[1], out tmp.y);
				float.TryParse(strs[2], out tmp.z);
				return tmp;
			});
			return tmp;
		}

		public static Vector3Int LoadVector3Int(string key, Vector3Int defaultValue)
		{
			Vector3Int tmp = defaultValue;
			LoadVectorAndSplit<Vector3Int>(ref tmp, key, onSplit: (strs, tmp) =>
			{
				int tmpx = tmp.x;
				int tmpy = tmp.y;
				int tmpz = tmp.z;
				int.TryParse(strs[0], out tmpx);
				int.TryParse(strs[1], out tmpy);
				int.TryParse(strs[2], out tmpz);
				return new Vector3Int(tmpx, tmpy, tmpz);
			});
			return tmp;
		}

		public static Vector4 LoadVector4(string key, Vector4 defaultValue)
		{
			Vector4 tmp = defaultValue;
			LoadVectorAndSplit<Vector4>(ref tmp, key, onSplit: (strs, tmp) =>
			{
				float.TryParse(strs[0], out tmp.x);
				float.TryParse(strs[1], out tmp.y);
				float.TryParse(strs[2], out tmp.z);
				float.TryParse(strs[3], out tmp.w);
				return tmp;
			});
			return tmp;
		}
	}
}
