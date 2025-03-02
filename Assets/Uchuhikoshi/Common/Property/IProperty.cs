using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Uchuhikoshi
{
    public interface IProperty
    {
		void AddTo(List<IProperty> list);
		void Reset();
	}

	public interface IProperty<T> : IProperty
	{
		event Action<T?>? OnValueChanged;
		T? Value { get; set; }

		T? OnSetDefault();
    }

	public interface IPropertyStorable : IProperty
	{
	}

	public interface IPropertyStorable<T> : IPropertyStorable, IProperty
	{
	}
}
