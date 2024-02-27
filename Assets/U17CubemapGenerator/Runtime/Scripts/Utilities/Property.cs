using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

#nullable enable
#pragma warning disable 8604 // Possible null reference argument for parameter '' in ''

namespace Uchuhikoshi
{
	public class PropertyBase<T>
	{
		protected T? _value;
		readonly string? _prefsKey;
		public Action<T>? onValueChanged;
		public Action<string, T?>? onSaveValue;

		public PropertyBase(string? prefsKey, T? def = default(T),
			Func<string, T?, T?>? onLoadValue = null,
			Action<string, T?>? onSaveValue = null)
		{
			_prefsKey = prefsKey;
			if (!string.IsNullOrEmpty(_prefsKey) && onLoadValue != null)
			{
				_value = onLoadValue.Invoke(_prefsKey, def);
			}
			else
			{
				_value = def;
			}
			this.onSaveValue = onSaveValue;
		}

		public T? value
		{
			get => _value;
			set
			{
				if (!Equals(value))
				{
					_value = value;
					onValueChanged?.Invoke(value);
					if (!string.IsNullOrEmpty(_prefsKey))
					{
						onSaveValue?.Invoke(_prefsKey, value);
					}
				}
			}
		}

		protected virtual bool Equals(T? other)
		{
			return IEquatable<bool>.Equals(_value, other);
		}
	}

	public class PropertyBool : PropertyBase<bool>
		{
		public PropertyBool(string? prefsKey, bool def = default)
			: base(prefsKey, def,
			onLoadValue: PropertyKeyValue.LoadBool,
			onSaveValue: PropertyKeyValue.SaveBool)
		{}
		protected override bool Equals(bool other) => (_value == other);
	}

	public class PropertyInt : PropertyBase<int>
	{
		public PropertyInt(string? prefsKey, int def = default)
			: base(prefsKey, def,
			onLoadValue: PropertyKeyValue.LoadInt,
			onSaveValue: PropertyKeyValue.SaveInt)
		{}
		protected override bool Equals(int other) => (_value == other);
	}

	public class PropertyFloat : PropertyBase<float>
	{
		public PropertyFloat(string? prefsKey, float def = default)
			: base(prefsKey, def,
			onLoadValue: PropertyKeyValue.LoadFloat,
			onSaveValue: PropertyKeyValue.SaveFloat)
		{}
		protected override bool Equals(float other) => Mathf.Approximately(_value, other);
	}

	public class PropertyString : PropertyBase<string>
	{
		public PropertyString(string? prefsKey, string def = "")
			: base(prefsKey, def,
			onLoadValue: (prefsKey_, def_) => PropertyKeyValue.LoadString(prefsKey_, def_),
			onSaveValue: (prefsKey_, value_) => PropertyKeyValue.SaveString(prefsKey_, value_))
		{ }
		protected override bool Equals(string? other) =>
			(_value != null && other != null) ? _value.Equals(other) : (_value == null && other == null);

		public new string value
		{
			get => base.value!;
			set => base.value = value;
		}
	}

	public class PropertyEnum<T> : PropertyBase<T> where T : struct
	{
		public PropertyEnum(string? prefsKey, T def = default(T))
			: base(prefsKey, def,
			onLoadValue: (prefsKey_, def_) => (T)(object)PropertyKeyValue.LoadInt(prefsKey_, (int)(object)def_),
			onSaveValue: (prefsKey_, value_) => PropertyKeyValue.SaveInt(prefsKey_, (int)(object)value_))
		{ }
	}

	public class PropertyAsset<T> : PropertyBase<T> where T : UnityEngine.Object
	{
		public PropertyAsset(string? prefsKey, T? def = default(T))
			: base(prefsKey, def,
			onLoadValue: (prefsKey_, def_) =>
			{
#if UNITY_EDITOR
				return (T)AssetDatabase.LoadAssetAtPath<T>(PropertyKeyValue.LoadString(prefsKey_, string.Empty));
#else
				return default(T);
#endif
			},
			onSaveValue: (prefsKey_, value_) =>
			{
				string str = string.Empty;
#if UNITY_EDITOR
				if (value_ != null) { str = AssetDatabase.GetAssetPath(value_); }
#endif
				PropertyKeyValue.SaveString(prefsKey_, str);
			})
		{ }
	}

	public class PropertyUnityObject<T> : PropertyBase<T> where T : UnityEngine.Object
	{
		public PropertyUnityObject(string? prefsKey, T? def = default(T))
			: base(prefsKey, def,
			onLoadValue: null,
			onSaveValue: null)
		{ }
	}

	public static class PropertyKeyValue
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

		public static void SaveVector3(string key, Vector3 value)
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
