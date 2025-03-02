using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

#nullable enable

namespace Uchuhikoshi
{
	public abstract class PropertyStorableBase<T> : IPropertyStorable<T>, IProperty<T>
	{
		protected T? _value;
		readonly Func<T?>? _onSetDefault;
		public event Action<T?>? OnValueChanged;

		readonly string _prefsKey;
		public event Action<string, T?>? OnSaveValue;

		public T? OnSetDefault() => default(T?);

		protected PropertyStorableBase(
				string prefsKey,
				Func<string, T?,T?>? onLoadValue,
				Action<string, T?>? onSaveValue,
				List<IProperty>? list,
				Func<T?>? onSetDefault,
				Func<T?>? onSetInitial,
				Action<T>? onValueChanged)
		{
			if (string.IsNullOrEmpty(prefsKey)) 
			{
				throw new ArgumentNullException(nameof(prefsKey));
			}
			if (onSetInitial == null)
			{
				onSetInitial = (onSetDefault != null) ? onSetDefault : OnSetDefault;
			}
			T? def = onSetInitial.Invoke();

			_onSetDefault = onSetDefault;
			this.OnValueChanged += onValueChanged;
			this.OnSaveValue = onSaveValue;
			_prefsKey = prefsKey;
			if (onLoadValue != null)
			{
				_value = onLoadValue.Invoke(_prefsKey, def);
			}
			else
			{
				_value = def;
			}

			if (list != null)
			{
				AddTo(list);
			}
		}

		public T? Value
		{
			get => _value;
			set
			{
				if (!Equals(value))
				{
					_value = value;
					this.OnValueChanged?.Invoke(value);
					if (this.OnSaveValue != null)
					{
						this.OnSaveValue.Invoke(_prefsKey, value);
					}
				}
			}
		}

		protected virtual bool Equals(T? other)
		{
			Debug.LogWarning($"Boxing: {typeof(T).Name} If comparisons are to be made frequently, it is recommended to create a dedicated class.");
			return IEquatable<bool>.Equals(_value, other);
		}

		public void AddTo(List<IProperty> list)
		{
			list.Add(this);
		}

		public virtual void Reset()
		{
			if (_onSetDefault != null)
			{
				this.Value = _onSetDefault.Invoke();
			}
		}
	}

	public class PropertyStorableBool : PropertyStorableBase<bool>
	{
		public PropertyStorableBool(
			string prefsKey,
			List<IProperty>? list = null,
			Func<bool>? onSetDefault = null, Func<bool>? onSetInitial = null, Action<bool>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => KeyValueStore.LoadBool(prefsKey_, def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveBool(prefsKey_, value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{
		}
		protected override bool Equals(bool other) => (_value == other);
	}

	public sealed class PropertyStorableInt : PropertyStorableBase<int>
	{
		public PropertyStorableInt(
			string prefsKey,
			List<IProperty>? list = null,
			Func<int>? onSetDefault = null, Func<int>? onSetInitial = null, Action<int>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => KeyValueStore.LoadInt(prefsKey_, def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveInt(prefsKey_, value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(int other) => (_value == other);
	}

	public sealed class PropertyStorableFloat : PropertyStorableBase<float>
	{
		public PropertyStorableFloat(
			string prefsKey,
			List<IProperty>? list = null,
			Func<float>? onSetDefault = null, Func<float>? onSetInitial = null, Action<float>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => KeyValueStore.LoadFloat(prefsKey_, def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveFloat(prefsKey_, value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(float other) => Mathf.Approximately(_value, other);
	}

	public sealed class PropertyStorableVector2 : PropertyStorableBase<Vector2>
	{
		public PropertyStorableVector2(
			string prefsKey,
			List<IProperty>? list = null, 
			Func<Vector2>? onSetDefault = null, Func<Vector2>? onSetInitial = null, Action<Vector2>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => KeyValueStore.LoadVector2(prefsKey_, def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveVector2(prefsKey_, value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(Vector2 other) =>
			Mathf.Approximately(_value.x, other.x) &&
			Mathf.Approximately(_value.y, other.y);
	}

	public sealed class PropertyStorableVector2Int : PropertyStorableBase<Vector2Int>
	{
		public PropertyStorableVector2Int(
			string prefsKey,
			List<IProperty>? list = null, 
			Func<Vector2Int>? onSetDefault = null, Func<Vector2Int>? onSetInitial = null, Action<Vector2Int>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => KeyValueStore.LoadVector2Int(prefsKey_, def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveVector2Int(prefsKey_, value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(Vector2Int other) =>
			(_value.x == other.x) &&
			(_value.y == other.y);
	}

	public sealed class PropertyStorableVector3 : PropertyStorableBase<Vector3>
	{
		public PropertyStorableVector3(
			string prefsKey,
			List<IProperty>? list = null, 
			Func<Vector3>? onSetDefault = null, Func<Vector3>? onSetInitial = null, Action<Vector3>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => KeyValueStore.LoadVector3(prefsKey_, def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveVector3(prefsKey_, value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(Vector3 other) =>
			Mathf.Approximately(_value.x, other.x) &&
			Mathf.Approximately(_value.y, other.y) &&
			Mathf.Approximately(_value.z, other.z);
	}

	public sealed class PropertyStorableVector3Int : PropertyStorableBase<Vector3Int>
	{
		public PropertyStorableVector3Int(
			string prefsKey,
			List<IProperty>? list = null,
			Func<Vector3Int>? onSetDefault = null, Func<Vector3Int>? onSetInitial = null, Action<Vector3Int>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => KeyValueStore.LoadVector3Int(prefsKey_, def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveVector3Int(prefsKey_, value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(Vector3Int other) =>
			(_value.x == other.x) &&
			(_value.y == other.y) &&
			(_value.z == other.z);
	}

	public sealed class PropertyStorableVector4 : PropertyStorableBase<Vector4>
	{
		public PropertyStorableVector4(
			string prefsKey,
			List<IProperty>? list = null,
			Func<Vector4>? onSetDefault = null, Func<Vector4>? onSetInitial = null, Action<Vector4>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => KeyValueStore.LoadVector4(prefsKey_, def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveVector4(prefsKey_, value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(Vector4 other) =>
			Mathf.Approximately(_value.x, other.x) &&
			Mathf.Approximately(_value.y, other.y) &&
			Mathf.Approximately(_value.z, other.z) &&
			Mathf.Approximately(_value.w, other.w);
	}

	public sealed class PropertyStorableString : PropertyStorableBase<string>
	{
		public PropertyStorableString(
			string prefsKey,
			List<IProperty>? list = null,
			Func<string>? onSetDefault = null, Func<string>? onSetInitial = null, Action<string>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => KeyValueStore.LoadString(prefsKey_, ((def_ != null) ? def_ : "")),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveString(prefsKey_, ((value_ != null) ? value_ : "")),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(string? other) =>
			(_value != null && other != null) ? _value.Equals(other) : (_value == null && other == null);

		public new string Value
		{
			get => base.Value!;
			set => base.Value = value;
		}
	}

	public sealed class PropertyStorableEnum<T> : PropertyStorableBase<T> where T : struct
	{
		public PropertyStorableEnum(
			string prefsKey,
			List<IProperty>? list = null,
			Func<T>? onSetDefault = null, Func<T>? onSetInitial = null, Action<T>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => (T)(object)KeyValueStore.LoadInt(prefsKey_, (int)(object)def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveInt(prefsKey_, (int)(object)value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(T other) => ((int)(object)_value == (int)(object)other);
	}

	public sealed class PropertyStorableAsset<T> : PropertyStorableBase<T> where T : UnityEngine.Object
	{
		public PropertyStorableAsset(
			string prefsKey,
			List<IProperty>? list = null,
			Func<T?>? onSetDefault = null, Func<T?>? onSetInitial = null, Action<T>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) =>
				{
#if UNITY_EDITOR
					return (T)AssetDatabase.LoadAssetAtPath<T>(KeyValueStore.LoadString(prefsKey_, string.Empty));
#else
					return default(T);
#endif
				},
				onSaveValue: (prefsKey_, value_) => {
					string str = string.Empty;
#if UNITY_EDITOR
					if (value_ != null) { str = AssetDatabase.GetAssetPath(value_); }
#endif
					KeyValueStore.SaveString(prefsKey_, str);
				},
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
	}

}
