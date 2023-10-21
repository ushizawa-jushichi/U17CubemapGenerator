using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

#nullable enable
#pragma warning disable IDE1006 // naming rule

namespace Ushino17
{
	public sealed class U17CubemapGeneratorEditorContext
	{
		public PreviewScene previewScene { get; private set; }
		public U17CubemapGenerator? generatorInstance { get; private set; }

		static readonly string AssetPathCubemapGeneratorPrefab = AssetPath.U17CubemapGenerator + "Editor/Assets/u17cubemap_generator.prefab";

		static class SettingsKeys
		{
			public const string HideOtherUI = "U17CubemapGenerator.HideOtherUI";
			public const string ShowSkyBox = "U17CubemapGenerator.ShowSkyBox";
			public const string SuperSampling = "U17CubemapGenerator.SuperSampling";
			public const string PreviewObject = "U17CubemapGenerator.PreviewObject";
			public const string InputSource = "U17CubemapGenerator.InputSource";
			public const string OutputLayout = "U17CubemapGenerator.OutputLayout";
			public const string TextureWidth = "U17CubemapGenerator.TextureWidth";
			public const string EquirectangularRotation = "U17CubemapGenerator.EquirectangularRotation";
			public const string OutputHDR = "U17CubemapGenerator.OutputHDR";
			public const string OutputSRGB = "U17CubemapGenerator.OutputSRGB";
			public const string OutputCubemap = "U17CubemapGenerator.OutputCubemap";
			public const string OutputGenerateMipmap = "U17CubemapGenerator.OutputGenerateMipmap";
			public const string FillMatcapOutsideByEdgeColor = "U17CubemapGenerator.FillMatcapOutsideByEdgeColor";
			public const string Language = "U17CubemapGenerator.Language";
			public const string LastAssetPath = "U17CubemapGenerator.LastAssetPath";
			public const string LastAssetCubemap = "U17CubemapGenerator.LastAssetCubemap";
			public const string LastAssetTextureLeft = "U17CubemapGenerator.LastAssetTextureLeft";
			public const string LastAssetTextureRight = "U17CubemapGenerator.LastAssetTextureRight";
			public const string LastAssetTextureTop = "U17CubemapGenerator.LastAssetTextureTop";
			public const string LastAssetTextureBottom = "U17CubemapGenerator.LastAssetTextureBottom";
			public const string LastAssetTextureFront = "U17CubemapGenerator.LastAssetTextureFront";
			public const string LastAssetTextureBack = "U17CubemapGenerator.LastAssetTextureBack";
			public const string UsingCameraAngles = "U17CubemapGenerator.UsingCameraAngles";
			public const string HorizontalRotation = "U17CubemapGenerator.HorizontalRotation";
			public const string ExposureOverride = "U17CubemapGenerator.ExposureOverride";
			public const string FixedExposure = "U17CubemapGenerator.FixedExposure";
			public const string Compensation = "U17CubemapGenerator.Compensation";
		}

		readonly EasyLocalization _localization = new EasyLocalization();

		bool _requestRedraw;
		string _adviceMessage = string.Empty;
		readonly PropertyBool _hideOtherUIProp = new PropertyBool(SettingsKeys.HideOtherUI, false);
		readonly PropertyBool _showSkyBoxProp = new PropertyBool(SettingsKeys.ShowSkyBox, false);
		readonly PropertyBool _superSamplingProp = new PropertyBool(SettingsKeys.SuperSampling, true);
		readonly PropertyPreviewObjectType _previewObjectProp = new PropertyPreviewObjectType(SettingsKeys.PreviewObject, PreviewObjectType.Sphere);
		readonly PropertyInputSource _inputSourceProp = new PropertyInputSource(SettingsKeys.InputSource, U17CubemapGenerator.InputSource.CurrentScene);
		readonly PropertyOutputLayout _outputLayoutProp = new PropertyOutputLayout(SettingsKeys.OutputLayout, U17CubemapGenerator.OutputLayout.CrossHorizontal);
		readonly PropertyTextureWidthType _textureWidthProp = new PropertyTextureWidthType(SettingsKeys.TextureWidth, TextureWidthType._1024);
		readonly PropertyFloat _equirectangularRotationProp = new PropertyFloat(SettingsKeys.EquirectangularRotation, 0f);
		readonly PropertyBool _outputHDRProp = new PropertyBool(SettingsKeys.OutputHDR, false);
		readonly PropertyBool _outputSRGBProp = new PropertyBool(SettingsKeys.OutputSRGB, true);
		readonly PropertyBool _outputCubemapProp = new PropertyBool(SettingsKeys.OutputCubemap, true);
		readonly PropertyBool _outputGenerateMipmapProp = new PropertyBool(SettingsKeys.OutputGenerateMipmap, true);
		readonly PropertyBool _fillMatcapOutsideByEdgeColorProp = new PropertyBool(SettingsKeys.FillMatcapOutsideByEdgeColor, false);
		readonly PropertyCamera _specificCameraProp = new PropertyCamera(null);
		string _lastAssetPath = string.Empty;
		readonly PropertyAssetCubemap _cubemapProp = new PropertyAssetCubemap(SettingsKeys.LastAssetCubemap, null);
		readonly PropertyAssetTexture2D _textureLeftProp = new PropertyAssetTexture2D(SettingsKeys.LastAssetTextureLeft, null);
		readonly PropertyAssetTexture2D _textureRightProp = new PropertyAssetTexture2D(SettingsKeys.LastAssetTextureRight, null);
		readonly PropertyAssetTexture2D _textureTopProp = new PropertyAssetTexture2D(SettingsKeys.LastAssetTextureTop, null);
		readonly PropertyAssetTexture2D _textureBottomProp = new PropertyAssetTexture2D(SettingsKeys.LastAssetTextureBottom, null);
		readonly PropertyAssetTexture2D _textureFrontProp = new PropertyAssetTexture2D(SettingsKeys.LastAssetTextureFront, null);
		readonly PropertyAssetTexture2D _textureBackProp = new PropertyAssetTexture2D(SettingsKeys.LastAssetTextureBack, null);
		readonly PropertySystemLanguage _languageProp;
		readonly PropertyBool _usingCameraAnglesProp = new PropertyBool(SettingsKeys.UsingCameraAngles, false);
		readonly PropertyRotationAngleType _horizontalRotationProp = new PropertyRotationAngleType(SettingsKeys.HorizontalRotation, RotationAngleType._0);
		readonly PropertyBool _exposureOverrideProp = new PropertyBool(SettingsKeys.ExposureOverride, U17CubemapGenerator.ExposureOverrideEnabledDefault);
		readonly PropertyFloat _fixedExposureProp = new PropertyFloat(SettingsKeys.FixedExposure, U17CubemapGenerator.FixedExposureStandard);
		readonly PropertyFloat _compensationProp = new PropertyFloat(SettingsKeys.Compensation, 0f);

		public string adviceMessage => _adviceMessage;
		public bool hideOtherUI { get => _hideOtherUIProp.value; set => _hideOtherUIProp.value = value; }
		public bool showSkyBox { get => _showSkyBoxProp.value; set => _showSkyBoxProp.value = value; }
		public bool superSampling { get => _superSamplingProp.value; set => _superSamplingProp.value = value; }
		public PreviewObjectType previewObject { get => _previewObjectProp.value; set => _previewObjectProp.value = value; }
		public U17CubemapGenerator.InputSource inputSource { get => _inputSourceProp.value; set => _inputSourceProp.value = value; }
		public U17CubemapGenerator.OutputLayout outputLayout { get => _outputLayoutProp.value; set => _outputLayoutProp.value = value; }
		public TextureWidthType textureWidth { get => _textureWidthProp.value; set => _textureWidthProp.value = value; }
		public float equirectangularRotation { get => _equirectangularRotationProp.value; set => _equirectangularRotationProp.value = value; }
		public bool outputHDR { get => _outputHDRProp.value; set => _outputHDRProp.value = value; }
		public bool outputSRGB { get => _outputSRGBProp.value; set => _outputSRGBProp.value = value; }
		public bool outputCubemap { get => _outputCubemapProp.value; set => _outputCubemapProp.value = value; }
		public bool outputGenerateMipmap { get => _outputGenerateMipmapProp.value; set => _outputGenerateMipmapProp.value = value; }
		public bool fillMatcapOutsideByEdgeColor { get => _fillMatcapOutsideByEdgeColorProp.value; set => _fillMatcapOutsideByEdgeColorProp.value = value; }
		public SystemLanguage language { get => _languageProp.value; set => _languageProp.value = value; }
		public Camera? specificCamera { get => _specificCameraProp.value; set => _specificCameraProp.value = value; }
		public Cubemap? cubemap { get => _cubemapProp.value; set => _cubemapProp.value = value; }
		public Texture2D? textureLeft { get => _textureLeftProp.value; set => _textureLeftProp.value = value; }
		public Texture2D? textureRight { get => _textureRightProp.value; set => _textureRightProp.value = value; }
		public Texture2D? textureTop { get => _textureTopProp.value; set => _textureTopProp.value = value; }
		public Texture2D? textureBottom { get => _textureBottomProp.value; set => _textureBottomProp.value = value; }
		public Texture2D? textureFront { get => _textureFrontProp.value; set => _textureFrontProp.value = value; }
		public Texture2D? textureBack { get => _textureBackProp.value; set => _textureBackProp.value = value; }
		public bool usingCameraAngles { get => _usingCameraAnglesProp.value; set => _usingCameraAnglesProp.value = value; }
		public RotationAngleType horizontalRotation { get => _horizontalRotationProp.value; set => _horizontalRotationProp.value = value; }
		public bool isSourceHDR { get => (this.generatorInstance != null) && this.generatorInstance.isSourceHDR; }
		public bool exposureOverride { get => _exposureOverrideProp.value; set => _exposureOverrideProp.value = value; }
		public float fixedExposure { get => _fixedExposureProp.value; set => _fixedExposureProp.value = value; }
		public float compensation { get => _compensationProp.value; set => _compensationProp.value = value; }

		public Action<SystemLanguage>? onLanguageChanged;
		public IReadOnlyList<SystemLanguage> supportedLanguages => _localization.supportedLanguages;

		public RenderPipelineUtils.PipelineType pipelineType => (this.generatorInstance != null) ? this.generatorInstance.pipelineType : RenderPipelineUtils.PipelineType.Unsupported;

		public U17CubemapGeneratorEditorContext()
		{
			_lastAssetPath = PropertyKeyValue.LoadString(SettingsKeys.LastAssetPath, _lastAssetPath);
			this.previewScene = new PreviewScene();
			this.previewScene.camera.transform.position = new Vector3(0f, 0f, -2);

			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPathCubemapGeneratorPrefab);
			if (prefab == null)
			{
				throw new InvalidOperationException($"{AssetPathCubemapGeneratorPrefab} not found");
			}
			var goGenerator = UnityEngine.Object.Instantiate(prefab);
			goGenerator.hideFlags = HideFlags.HideAndDontSave;
			this.generatorInstance = goGenerator.GetComponent<U17CubemapGenerator>();
			this.generatorInstance.SetEditorDragControl(true);
			this.generatorInstance.onPipelineChanged += StartRedraw;

			this.generatorInstance.previewSphere.transform.SetParent(null);
			this.previewScene.AddGameObject(this.generatorInstance.previewSphere);
			this.generatorInstance.previewCube.transform.SetParent(null);
			this.previewScene.AddGameObject(this.generatorInstance.previewCube);

			_languageProp = new PropertySystemLanguage(SettingsKeys.Language, _localization.language);
			_localization.language = _languageProp.value;
			_languageProp.onValueChanged += (value) =>
			{
				_localization.language = value;
				this.onLanguageChanged?.Invoke(value);
			};

			_specificCameraProp.onValueChanged += (_) => RequestRedraw();
			_previewObjectProp.onValueChanged += (_) => UpdatePreviewObject();
			_inputSourceProp.onValueChanged += (_) => RequestRedraw();
			_textureWidthProp.onValueChanged += (_) => RequestRedraw();
			_cubemapProp.onValueChanged += (_) => RequestRedraw();
			_textureLeftProp.onValueChanged += (_) => RequestRedraw();
			_textureRightProp.onValueChanged += (_) => RequestRedraw();
			_textureTopProp.onValueChanged += (_) => RequestRedraw();
			_textureBottomProp.onValueChanged += (_) => RequestRedraw();
			_textureFrontProp.onValueChanged += (_) => RequestRedraw();
			_textureBackProp.onValueChanged += (_) => RequestRedraw();
			_usingCameraAnglesProp.onValueChanged += (_) => RequestRedraw();
			_horizontalRotationProp.onValueChanged += (_) => RequestRedraw();
			_exposureOverrideProp.onValueChanged += (_) => RequestRedraw();
			_fixedExposureProp.onValueChanged += (_) => RequestRedraw();
			_compensationProp.onValueChanged += (_) => RequestRedraw();

			this.generatorInstance.SetPreviewObject(U17CubemapGenerator.PreviewObject.None);
			RequestRedraw();
			// UpdatePreviewObject() is called when redraw is completed by RequestRedraw
		}

		public void Dispose()
		{
			this.previewScene?.Dispose();
			this.previewScene = null!;
			if (this.generatorInstance != null)
			{
				UnityEngine.Object.DestroyImmediate(this.generatorInstance);
				this.generatorInstance = null;
			}
		}

		public void OnUpdate(float deltaTime)
		{
			if (this.generatorInstance == null || this.generatorInstance.isProcessing)
			{
				return;
			}

			if (_requestRedraw)
			{
				_requestRedraw = false;
				StartRedraw();
			}

			UpdateAnimation(deltaTime);
		}

		public void RequestRedraw()
		{
			_requestRedraw = true;
		}

		void UpdateAnimation(float deltaTime)
		{
		}

		void StartRedraw()
		{
			if (this.generatorInstance == null) throw new InvalidOperationException();

			if (!CanRender())
			{
				ClearCubemap();
				return;
			}
#if USING_HDRP
			if (this.pipelineType == RenderPipelineUtils.PipelineType.HDPipeline)
			{
				this.generatorInstance.SetExposureOverride(this.exposureOverride, this.fixedExposure, this.compensation);
			}
#endif

			switch (this.inputSource)
			{
				case U17CubemapGenerator.InputSource.CurrentScene:
					this.generatorInstance.SetHorizontalRotation(_rotationAngleArray[(int)this.horizontalRotation]);
					this.generatorInstance.SetUsingCameraAngles(this.usingCameraAngles);
					this.generatorInstance.SetSpecificCamera(this.specificCamera);
					this.generatorInstance.SetCubemapWidth(_textureWidthArray[(int)this.textureWidth]);
					break;

				case U17CubemapGenerator.InputSource.Cubemap:
					this.generatorInstance.SetSourceCubemap(this.cubemap!);
					break;

				case U17CubemapGenerator.InputSource.SixSided:
					var textures = GetSixSidedTextures();
					for (int i = 0; i < textures.Length; i++)
					{
						this.generatorInstance.SetSourceTexture((CubemapFace)i, textures[i]!);
					}
					break;
			}

			this.generatorInstance.StartRender(this.inputSource, onCompleted: () =>
			{
				UpdatePreviewObject();
			});
		}

		void UpdatePreviewObject()
		{
			if (this.generatorInstance == null) throw new InvalidOperationException();

			switch (_previewObjectProp.value)
			{
				case PreviewObjectType.Sphere:
					this.generatorInstance.SetPreviewObject(U17CubemapGenerator.PreviewObject.Sphere);
					break;
				case PreviewObjectType.Cube:
					this.generatorInstance.SetPreviewObject(U17CubemapGenerator.PreviewObject.Cube);
					break;
			}
		}

		public void ResetPreviewRotation()
		{
			if (this.generatorInstance == null) throw new InvalidOperationException();

			this.generatorInstance.ResetPreviewCubeRotation();
		}

		public void ExportSaveCubemap()
		{
			if (this.generatorInstance == null) throw new InvalidOperationException();

			this.generatorInstance.SetFillMatcapOutsideByEdgeColor(this.fillMatcapOutsideByEdgeColor);
			this.generatorInstance.SetOutputDesirableHDR(this.outputHDR);
			this.generatorInstance.SetOutputDesirableSRGB(this.outputSRGB);
			this.generatorInstance.SetOutputDesirableCubemap(this.outputCubemap);
			this.generatorInstance.SetOutputDesirableGenerateMipmap(this.outputGenerateMipmap);
			this.generatorInstance.SetCubemapOutputEquirectangularRotation(this.equirectangularRotation);
			this.generatorInstance.SetCubemapOutputLayout(this.outputLayout);
			this.generatorInstance.SaveAsset(_lastAssetPath, onCompleted: (path) =>
			{
				_lastAssetPath = path;
				PropertyKeyValue.SaveString(SettingsKeys.LastAssetPath, _lastAssetPath);
			});
		}

		public void ClearCubemap()
		{
			if (this.generatorInstance == null) throw new InvalidOperationException();

			this.generatorInstance.ClearCubemap();
		}

		Texture2D?[] GetSixSidedTextures()
		{
			return new Texture2D?[] { this.textureLeft, this.textureRight, this.textureTop, this.textureBottom, this.textureFront, this.textureBack };
		}

		public bool CanRender()
		{
			switch (this.inputSource)
			{
				case U17CubemapGenerator.InputSource.Cubemap:
					if (this.cubemap == null)
					{
						_adviceMessage = GetText(TextId.AdviceSetCubemap);
						return false;
					}
					// ok
					break;

				case U17CubemapGenerator.InputSource.SixSided:
					var textures = GetSixSidedTextures();
					int requiredWidth = 0;
					TextureFormat requiredFormat = TextureFormat.RGBA32;
					for (int i = 0; i < textures.Length; i++)
					{
						var tex = textures[i];
						if (tex == null)
						{
							_adviceMessage = GetText(TextId.AdviceSetSixSidedTexture);
							return false;
						}
						if (i == 0)
						{
							if (tex.width != tex.height)
							{
								_adviceMessage = GetText(TextId.AdviceTextureWidthAndHeightEqual);
								return false;
							}
							requiredWidth = tex.width;
							requiredFormat = tex.format;
						}
						else
						{
							if (tex.width != requiredWidth ||
								tex.height != requiredWidth)
							{
								_adviceMessage = GetText(TextId.AdviceEachTextureFaceSameSize);
								return false;
							}
							if (tex.format != requiredFormat)
							{
								_adviceMessage = GetText(TextId.AdviceEachTextureFacesSameFormat);
								return false;
							}
						}
					}
					// ok
					break;

				default:
					break;
			}
			// ok
			_adviceMessage = string.Empty;
			return true;
		}

		public string GetText(TextId id)
		{
			return _localization.Get(id);
		}

		public enum PreviewObjectType { Sphere, Cube, }
		public class PropertyPreviewObjectType : PropertyBase<PreviewObjectType>
		{
			public PropertyPreviewObjectType(string? prefsKey, PreviewObjectType def = default(PreviewObjectType))
				: base(prefsKey, def,
				onLoadValue: (prefsKey_, def_) => (PreviewObjectType)PropertyKeyValue.LoadInt(prefsKey_, (int)def_),
				onSaveValue: (prefsKey_, value_) => PropertyKeyValue.SaveInt(prefsKey_, (int)value_))
			{ }
			protected override bool Equals(PreviewObjectType other) => _value == other;
		}

		public enum TextureWidthType { _64, _128, _256, _512, _1024, _2048 }
		readonly int[] _textureWidthArray = new[] { 64, 128, 256, 512, 1024, 2048, };
		public class PropertyTextureWidthType : PropertyBase<TextureWidthType>
		{
			public PropertyTextureWidthType(string? prefsKey, TextureWidthType def = default(TextureWidthType))
				: base(prefsKey, def,
				onLoadValue: (prefsKey_, def_) => (TextureWidthType)PropertyKeyValue.LoadInt(prefsKey_, (int)def_),
				onSaveValue: (prefsKey_, value_) => PropertyKeyValue.SaveInt(prefsKey_, (int)value_))
			{ }
			protected override bool Equals(TextureWidthType other) => _value == other;
		}

		public enum RotationAngleType { _0, _45, _90, _135, _180, _225, _270, _315 }
		readonly float[] _rotationAngleArray = new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
		public class PropertyRotationAngleType : PropertyBase<RotationAngleType>
		{
			public PropertyRotationAngleType(string? prefsKey, RotationAngleType def = default(RotationAngleType))
				: base(prefsKey, def,
				onLoadValue: (prefsKey_, def_) => (RotationAngleType)PropertyKeyValue.LoadInt(prefsKey_, (int)def_),
				onSaveValue: (prefsKey_, value_) => PropertyKeyValue.SaveInt(prefsKey_, (int)value_))
			{ }
			protected override bool Equals(RotationAngleType other) => _value == other;
		}

		public class PropertySystemLanguage : PropertyBase<SystemLanguage>
		{
			public PropertySystemLanguage(string? prefsKey, SystemLanguage def = default(SystemLanguage))
				: base(prefsKey, def,
				onLoadValue: (prefsKey_, def_) => (SystemLanguage)PropertyKeyValue.LoadInt(prefsKey_, (int)def_),
				onSaveValue: (prefsKey_, value_) => PropertyKeyValue.SaveInt(prefsKey_, (int)value_))
			{ }
			protected override bool Equals(SystemLanguage other) => _value == other;
		}

		public class PropertyInputSource : PropertyBase<U17CubemapGenerator.InputSource>
		{
			public PropertyInputSource(string? prefsKey, U17CubemapGenerator.InputSource def = default(U17CubemapGenerator.InputSource))
				: base(prefsKey, def,
				onLoadValue: (prefsKey_, def_) => (U17CubemapGenerator.InputSource)PropertyKeyValue.LoadInt(prefsKey_, (int)def_),
				onSaveValue: (prefsKey_, value_) => PropertyKeyValue.SaveInt(prefsKey_, (int)value_))
			{ }
			protected override bool Equals(U17CubemapGenerator.InputSource other) => _value == other;
		}

		public class PropertyOutputLayout : PropertyBase<U17CubemapGenerator.OutputLayout>
		{
			public PropertyOutputLayout(string? prefsKey, U17CubemapGenerator.OutputLayout def = default(U17CubemapGenerator.OutputLayout))
				: base(prefsKey, def,
				onLoadValue: (prefsKey_, def_) => (U17CubemapGenerator.OutputLayout)PropertyKeyValue.LoadInt(prefsKey_, (int)def_),
				onSaveValue: (prefsKey_, value_) => PropertyKeyValue.SaveInt(prefsKey_, (int)value_))
			{ }
			protected override bool Equals(U17CubemapGenerator.OutputLayout other) => _value == other;
		}

		public class PropertyCamera : PropertyBase<Camera>
		{
			public PropertyCamera(string? prefsKey, Camera? def = default(Camera))
				: base(prefsKey, def,
				onLoadValue: null,
				onSaveValue: null)
			{ }
			protected override bool Equals(Camera? other) => _value == other;
		}

		public class PropertyAssetTexture2D : PropertyBase<Texture2D>
		{
			public PropertyAssetTexture2D(string? prefsKey, Texture2D? def = default(Texture2D))
				: base(prefsKey, def,
				onLoadValue: (prefsKey_, def_) =>
				{
#if UNITY_EDITOR
					return (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(PropertyKeyValue.LoadString(prefsKey_, string.Empty));
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
					PropertyKeyValue.SaveString(prefsKey_, str);
				})
			{ }
			protected override bool Equals(Texture2D? other) => _value == other;
		}

		public class PropertyAssetCubemap : PropertyBase<Cubemap>
		{
			public PropertyAssetCubemap(string? prefsKey, Cubemap? def = default(Cubemap))
				: base(prefsKey, def,
				onLoadValue: (prefsKey_, def_) =>
				{
#if UNITY_EDITOR
					return (Cubemap)AssetDatabase.LoadAssetAtPath<Cubemap>(PropertyKeyValue.LoadString(prefsKey_, string.Empty));
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
					PropertyKeyValue.SaveString(prefsKey_, str);
				})
			{ }
			protected override bool Equals(Cubemap? other) => _value == other;
		}
	}
}
