using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#nullable enable
#pragma warning disable IDE1006 // naming rule

namespace Uchuhikoshi.U17CubemapGenerator
{
	public sealed partial class U17CubemapGeneratorWindowContext
	{
		public enum PreviewObjectType { Sphere, Cube, }
		public class PropertyStorablePreviewObjectType : PropertyStorableBase<PreviewObjectType>
		{
			public PropertyStorablePreviewObjectType(string prefsKey,
				List<IProperty>? list = null,
				Func<PreviewObjectType>? onSetDefault = null, Func<PreviewObjectType>? onSetInitial = null, Action<PreviewObjectType>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => (PreviewObjectType)KeyValueStore.LoadInt(prefsKey_, (int)def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveInt(prefsKey_, (int)value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
			{ }
			protected override bool Equals(PreviewObjectType other) => _value == other;
		}

		public enum TextureWidthType { _64, _128, _256, _512, _1024, _2048, _4096 }
		readonly int[] _textureWidthArray = new[] { 64, 128, 256, 512, 1024, 2048, 4096, };
		public class PropertyStorableTextureWidthType : PropertyStorableBase<TextureWidthType>
		{
			public PropertyStorableTextureWidthType(string prefsKey,
				List<IProperty>? list = null,
				Func<TextureWidthType>? onSetDefault = null, Func<TextureWidthType>? onSetInitial = null, Action<TextureWidthType>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => (TextureWidthType)KeyValueStore.LoadInt(prefsKey_, (int)def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveInt(prefsKey_, (int)value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
			{ }
			protected override bool Equals(TextureWidthType other) => _value == other;
		}

		public enum RotationAngleType { _0, _45, _90, _135, _180, _225, _270, _315 }
		readonly float[] _rotationAngleArray = new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
		public class PropertyStorableRotationAngleType : PropertyStorableBase<RotationAngleType>
		{
			public PropertyStorableRotationAngleType(string prefsKey,
				List<IProperty>? list = null,
				Func<RotationAngleType>? onSetDefault = null, Func<RotationAngleType>? onSetInitial = null, Action<RotationAngleType>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => (RotationAngleType)KeyValueStore.LoadInt(prefsKey_, (int)def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveInt(prefsKey_, (int)value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
			{ }
			protected override bool Equals(RotationAngleType other) => _value == other;
		}

		public class PropertyStorableSystemLanguage : PropertyStorableBase<SystemLanguage>
		{
			public PropertyStorableSystemLanguage(string prefsKey,
				List<IProperty>? list = null,
				Func<SystemLanguage>? onSetDefault = null, Func<SystemLanguage>? onSetInitial = null, Action<SystemLanguage>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => (SystemLanguage)KeyValueStore.LoadInt(prefsKey_, (int)def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveInt(prefsKey_, (int)value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
			{ }
			protected override bool Equals(SystemLanguage other) => _value == other;
		}

		public class PropertyStorableInputSource : PropertyStorableBase<U17CubemapGenerator.InputSource>
		{
			public PropertyStorableInputSource(string prefsKey,
				List<IProperty>? list = null,
				Func<U17CubemapGenerator.InputSource>? onSetDefault = null, Func<U17CubemapGenerator.InputSource>? onSetInitial = null, Action<U17CubemapGenerator.InputSource>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => (U17CubemapGenerator.InputSource)KeyValueStore.LoadInt(prefsKey_, (int)def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveInt(prefsKey_, (int)value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
			{ }
			protected override bool Equals(U17CubemapGenerator.InputSource other) => _value == other;
		}

		public class PropertyStorableOutputLayout : PropertyStorableBase<U17CubemapGenerator.OutputLayout>
		{
			public PropertyStorableOutputLayout(string prefsKey,
				List<IProperty>? list = null,
				Func<U17CubemapGenerator.OutputLayout>? onSetDefault = null, Func<U17CubemapGenerator.OutputLayout>? onSetInitial = null, Action<U17CubemapGenerator.OutputLayout>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) => (U17CubemapGenerator.OutputLayout)KeyValueStore.LoadInt(prefsKey_, (int)def_),
				onSaveValue: (prefsKey_, value_) => KeyValueStore.SaveInt(prefsKey_, (int)value_),
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
			{ }
			protected override bool Equals(U17CubemapGenerator.OutputLayout other) => _value == other;
		}

		public class PropertyCamera : PropertyBase<Camera>
		{
			public PropertyCamera(
				List<IProperty>? list = null,
				Func<Camera?>? onSetDefault = null, Func<Camera?>? onSetInitial = null, Action<Camera?>? onValueChanged = null)
			: base(
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
			{ }
			protected override bool Equals(Camera? other) => _value == other;
		}

		public class PropertyStorableAssetTexture2D : PropertyStorableBase<Texture2D>
		{
			public PropertyStorableAssetTexture2D(string prefsKey,
				List<IProperty>? list = null,
				Func<Texture2D?>? onSetDefault = null, Func<Texture2D?>? onSetInitial = null, Action<Texture2D?>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) =>
				{
#if UNITY_EDITOR
					return (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(KeyValueStore.LoadString(prefsKey_, string.Empty));
#else
					return default(Texture2D);
#endif
				},
				onSaveValue: (prefsKey_, value_) =>
				{
					string str = string.Empty;
#if UNITY_EDITOR
					if (value_ != null) { str = AssetDatabase.GetAssetPath(value_); }
#endif
					KeyValueStore.SaveString(prefsKey_, str);
				},
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
			{ }
			protected override bool Equals(Texture2D? other) => _value == other;
		}

		public class PropertyStorableAssetCubemap : PropertyStorableBase<Cubemap>
		{
			public PropertyStorableAssetCubemap(string prefsKey,
				List<IProperty>? list = null,
				Func<Cubemap?>? onSetDefault = null, Func<Cubemap?>? onSetInitial = null, Action<Cubemap?>? onValueChanged = null)
			: base(
				prefsKey: prefsKey,
				onLoadValue: (prefsKey_, def_) =>
				{
#if UNITY_EDITOR
					return (Cubemap)AssetDatabase.LoadAssetAtPath<Cubemap>(KeyValueStore.LoadString(prefsKey_, string.Empty));
#else
					return default(Cubemap);
#endif
				},
				onSaveValue: (prefsKey_, value_) =>
				{
					string str = string.Empty;
#if UNITY_EDITOR
					if (value_ != null) { str = AssetDatabase.GetAssetPath(value_); }
#endif
					KeyValueStore.SaveString(prefsKey_, str);
				},
				list: list, onSetDefault: onSetDefault, onSetInitial: onSetInitial, onValueChanged: onValueChanged)
			{ }
			protected override bool Equals(Cubemap? other) => _value == other;
		}
	}
}
