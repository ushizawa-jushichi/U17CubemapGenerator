using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Uchuhikoshi
{
	public class PropertyBase<T> : IProperty<T>
	{
		protected T? _value;
		readonly Func<T?>? _onSetDefault;
		public event Action<T?>? OnValueChanged;

		public T? OnSetDefault() => default(T?);

		protected PropertyBase(
				Func<T?>? onSetDefault,
				Func<T?>? onSetInitial,
				List<IProperty>? list,
				Action<T>? onValueChanged)
		{
			if (onSetInitial == null)
			{
				onSetInitial = (onSetDefault != null) ? onSetDefault : OnSetDefault;
			}
			T? def = onSetInitial.Invoke();

			_onSetDefault = onSetDefault;
			this.OnValueChanged += onValueChanged;
			_value = def;

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

	public class PropertyBool : PropertyBase<bool>
	{
		public PropertyBool(
			List<IProperty>? list = null,
			Func<bool>? onSetDefault = null, Func<bool>? onSetInitial = null, Action<bool>? onValueChanged = null)
			: base(
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{
		}
		protected override bool Equals(bool other) => (_value == other);
	}

	public sealed class PropertyInt : PropertyBase<int>
	{
		public PropertyInt(
			List<IProperty>? list = null,
			Func<int>? onSetDefault = null, Func<int>? onSetInitial = null, Action<int>? onValueChanged = null)
			: base(
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(int other) => (_value == other);
	}

	public sealed class PropertyFloat : PropertyBase<float>
	{
		public PropertyFloat(
			List<IProperty>? list = null,
			Func<float>? onSetDefault = null, Func<float>? onSetInitial = null, Action<float>? onValueChanged = null)
			: base(
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(float other) => Mathf.Approximately(_value, other);
	}

	public sealed class PropertyVector2 : PropertyBase<Vector2>
	{
		public PropertyVector2(
			List<IProperty>? list = null, 
			Func<Vector2>? onSetDefault = null, Func<Vector2>? onSetInitial = null, Action<Vector2>? onValueChanged = null)
			: base(
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(Vector2 other) =>
			Mathf.Approximately(_value.x, other.x) &&
			Mathf.Approximately(_value.y, other.y);
	}

	public sealed class PropertyVector2Int : PropertyBase<Vector2Int>
	{
		public PropertyVector2Int(
			List<IProperty>? list = null, 
			Func<Vector2Int>? onSetDefault = null, Func<Vector2Int>? onSetInitial = null, Action<Vector2Int>? onValueChanged = null)
			: base(
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(Vector2Int other) =>
			(_value.x == other.x) &&
			(_value.y == other.y);
	}

	public sealed class PropertyVector3 : PropertyBase<Vector3>
	{
		public PropertyVector3(
			List<IProperty>? list = null, 
			Func<Vector3>? onSetDefault = null, Func<Vector3>? onSetInitial = null, Action<Vector3>? onValueChanged = null)
			: base(
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(Vector3 other) =>
			Mathf.Approximately(_value.x, other.x) &&
			Mathf.Approximately(_value.y, other.y) &&
			Mathf.Approximately(_value.z, other.z);
	}

	public sealed class PropertyVector3Int : PropertyBase<Vector3Int>
	{
		public PropertyVector3Int(
			List<IProperty>? list = null,
			Func<Vector3Int>? onSetDefault = null, Func<Vector3Int>? onSetInitial = null, Action<Vector3Int>? onValueChanged = null)
			: base(
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(Vector3Int other) =>
			(_value.x == other.x) &&
			(_value.y == other.y) &&
			(_value.z == other.z);
	}

	public sealed class PropertyVector4 : PropertyBase<Vector4>
	{
		public PropertyVector4(
			List<IProperty>? list = null,
			Func<Vector4>? onSetDefault = null, Func<Vector4>? onSetInitial = null, Action<Vector4>? onValueChanged = null)
			: base(
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(Vector4 other) =>
			Mathf.Approximately(_value.x, other.x) &&
			Mathf.Approximately(_value.y, other.y) &&
			Mathf.Approximately(_value.z, other.z) &&
			Mathf.Approximately(_value.w, other.w);
	}

	public sealed class PropertyString : PropertyBase<string>
	{
		public PropertyString(
			List<IProperty>? list = null,
			Func<string>? onSetDefault = null, Func<string>? onSetInitial = null, Action<string>? onValueChanged = null)
			: base(
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

	public sealed class PropertyEnum<T> : PropertyBase<T> where T : struct
	{
		public PropertyEnum(
			List<IProperty>? list = null,
			Func<T>? onSetDefault = null, Func<T>? onSetInitial = null, Action<T>? onValueChanged = null)
			: base(
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
		protected override bool Equals(T other) => ((int)(object)_value == (int)(object)other);
	}

	public sealed class PropertyAsset<T> : PropertyBase<T> where T : UnityEngine.Object
	{
		public PropertyAsset(
			List<IProperty>? list = null,
			Func<T?>? onSetDefault = null, Func<T?>? onSetInitial = null, Action<T>? onValueChanged = null)
			: base(
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
	}

	public sealed class PropertyUnityObject<T> : PropertyBase<T> where T : UnityEngine.Object
	{
		public PropertyUnityObject(
			List<IProperty>? list = null,
			Func<T?>? onSetDefault = null, Func<T?>? onSetInitial = null, Action<T>? onValueChanged = null)
			: base(
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
	}

	public sealed class PropertyObject<T> : PropertyBase<T>
	{
		public PropertyObject(
			List<IProperty>? list = null,
			Func<T?>? onSetDefault = null, Func<T?>? onSetInitial = null, Action<T>? onValueChanged = null)
			: base(
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
		{}
	}
}
