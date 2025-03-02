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
		public PreviewScene PreviewScene { get; private set; }
		public U17CubemapGenerator? Generator { get; private set; }

		static readonly string AssetPathCubemapGeneratorPrefab = MyAssetPath.U17CubemapGenerator + "Editor/Assets/u17cubemap_generator.prefab";

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

		readonly EasyLocalization<TextId> _localization = new(MyAssetPath.U17CubemapGenerator);

		bool _requestRedraw;
		string _adviceMessage = string.Empty;
		readonly PropertyStorableBool _hideOtherUIProp = new PropertyStorableBool(SettingsKeys.HideOtherUI, onSetDefault: () => false);
		readonly PropertyStorableBool _showSkyBoxProp = new PropertyStorableBool(SettingsKeys.ShowSkyBox, onSetDefault: () => false);
		readonly PropertyStorableBool _superSamplingProp = new PropertyStorableBool(SettingsKeys.SuperSampling, onSetDefault: () => true);
		readonly PropertyStorablePreviewObjectType _previewObjectProp = new PropertyStorablePreviewObjectType(SettingsKeys.PreviewObject, onSetDefault: () => PreviewObjectType.Sphere);
		readonly PropertyStorableInputSource _inputSourceProp = new PropertyStorableInputSource(SettingsKeys.InputSource, onSetDefault: () => U17CubemapGenerator.InputSource.CurrentScene);
		readonly PropertyStorableOutputLayout _outputLayoutProp = new PropertyStorableOutputLayout(SettingsKeys.OutputLayout, onSetDefault: () => U17CubemapGenerator.OutputLayout.CrossHorizontal);
		readonly PropertyStorableTextureWidthType _textureWidthProp = new PropertyStorableTextureWidthType(SettingsKeys.TextureWidth, onSetDefault: () => TextureWidthType._1024);
		readonly PropertyStorableFloat _equirectangularRotationProp = new PropertyStorableFloat(SettingsKeys.EquirectangularRotation, onSetDefault: () => 0f);
		readonly PropertyStorableBool _outputHDRProp = new PropertyStorableBool(SettingsKeys.OutputHDR, onSetDefault: () => false);
		readonly PropertyStorableBool _outputSRGBProp = new PropertyStorableBool(SettingsKeys.OutputSRGB, onSetDefault: () => true);
		readonly PropertyStorableBool _outputCubemapProp = new PropertyStorableBool(SettingsKeys.OutputCubemap, onSetDefault: () => true);
		readonly PropertyStorableBool _outputGenerateMipmapProp = new PropertyStorableBool(SettingsKeys.OutputGenerateMipmap, onSetDefault: () => true);
		readonly PropertyStorableBool _fillMatcapOutsideByEdgeColorProp = new PropertyStorableBool(SettingsKeys.FillMatcapOutsideByEdgeColor, onSetDefault: () => false);
		readonly PropertyCamera _specificCameraProp = new PropertyCamera(null);
		string _lastAssetPath = string.Empty;
		readonly PropertyStorableAssetCubemap _cubemapProp = new PropertyStorableAssetCubemap(SettingsKeys.LastAssetCubemap, null);
		readonly PropertyStorableAssetTexture2D _textureLeftProp = new PropertyStorableAssetTexture2D(SettingsKeys.LastAssetTextureLeft, null);
		readonly PropertyStorableAssetTexture2D _textureRightProp = new PropertyStorableAssetTexture2D(SettingsKeys.LastAssetTextureRight, null);
		readonly PropertyStorableAssetTexture2D _textureTopProp = new PropertyStorableAssetTexture2D(SettingsKeys.LastAssetTextureTop, null);
		readonly PropertyStorableAssetTexture2D _textureBottomProp = new PropertyStorableAssetTexture2D(SettingsKeys.LastAssetTextureBottom, null);
		readonly PropertyStorableAssetTexture2D _textureFrontProp = new PropertyStorableAssetTexture2D(SettingsKeys.LastAssetTextureFront, null);
		readonly PropertyStorableAssetTexture2D _textureBackProp = new PropertyStorableAssetTexture2D(SettingsKeys.LastAssetTextureBack, null);
		readonly PropertyStorableSystemLanguage _languageProp;
		readonly PropertyStorableBool _usingCameraAnglesProp = new PropertyStorableBool(SettingsKeys.UsingCameraAngles, onSetDefault: () => false);
		readonly PropertyStorableRotationAngleType _horizontalRotationProp = new PropertyStorableRotationAngleType(SettingsKeys.HorizontalRotation, onSetDefault: () => RotationAngleType._0);
		readonly PropertyStorableBool _exposureOverrideProp = new PropertyStorableBool(SettingsKeys.ExposureOverride, onSetDefault: () => U17CubemapGenerator.ExposureOverrideEnabledDefault);
		readonly PropertyStorableFloat _fixedExposureProp = new PropertyStorableFloat(SettingsKeys.FixedExposure, onSetDefault: () => U17CubemapGenerator.FixedExposureStandard);
		readonly PropertyStorableFloat _compensationProp = new PropertyStorableFloat(SettingsKeys.Compensation, onSetDefault: () => 0f);

		public string AdviceMessage => _adviceMessage;
		public bool HideOtherUI { get => _hideOtherUIProp.Value; set => _hideOtherUIProp.Value = value; }
		public bool ShowSkyBox { get => _showSkyBoxProp.Value; set => _showSkyBoxProp.Value = value; }
		public bool SuperSampling { get => _superSamplingProp.Value; set => _superSamplingProp.Value = value; }
		public PreviewObjectType PreviewObject { get => _previewObjectProp.Value; set => _previewObjectProp.Value = value; }
		public U17CubemapGenerator.InputSource InputSource { get => _inputSourceProp.Value; set => _inputSourceProp.Value = value; }
		public U17CubemapGenerator.OutputLayout OutputLayout { get => _outputLayoutProp.Value; set => _outputLayoutProp.Value = value; }
		public TextureWidthType TextureWidth { get => _textureWidthProp.Value; set => _textureWidthProp.Value = value; }
		public float EquirectangularRotation { get => _equirectangularRotationProp.Value; set => _equirectangularRotationProp.Value = value; }
		public bool OutputHDR { get => _outputHDRProp.Value; set => _outputHDRProp.Value = value; }
		public bool OutputSRGB { get => _outputSRGBProp.Value; set => _outputSRGBProp.Value = value; }
		public bool OutputCubemap { get => _outputCubemapProp.Value; set => _outputCubemapProp.Value = value; }
		public bool OutputGenerateMipmap { get => _outputGenerateMipmapProp.Value; set => _outputGenerateMipmapProp.Value = value; }
		public bool FillMatcapOutsideByEdgeColor { get => _fillMatcapOutsideByEdgeColorProp.Value; set => _fillMatcapOutsideByEdgeColorProp.Value = value; }
		public SystemLanguage Language { get => _languageProp.Value; set => _languageProp.Value = value; }
		public Camera? SpecificCamera { get => _specificCameraProp.Value; set => _specificCameraProp.Value = value; }
		public Cubemap? Cubemap { get => _cubemapProp.Value; set => _cubemapProp.Value = value; }
		public Texture2D? TextureLeft { get => _textureLeftProp.Value; set => _textureLeftProp.Value = value; }
		public Texture2D? TextureRight { get => _textureRightProp.Value; set => _textureRightProp.Value = value; }
		public Texture2D? TextureTop { get => _textureTopProp.Value; set => _textureTopProp.Value = value; }
		public Texture2D? TextureBottom { get => _textureBottomProp.Value; set => _textureBottomProp.Value = value; }
		public Texture2D? TextureFront { get => _textureFrontProp.Value; set => _textureFrontProp.Value = value; }
		public Texture2D? TextureBack { get => _textureBackProp.Value; set => _textureBackProp.Value = value; }
		public bool UsingCameraAngles { get => _usingCameraAnglesProp.Value; set => _usingCameraAnglesProp.Value = value; }
		public RotationAngleType HorizontalRotation { get => _horizontalRotationProp.Value; set => _horizontalRotationProp.Value = value; }
		public bool IsSourceHDR { get => (this.Generator != null) && this.Generator.IsSourceHDR; }
		public bool ExposureOverride { get => _exposureOverrideProp.Value; set => _exposureOverrideProp.Value = value; }
		public float FixedExposure { get => _fixedExposureProp.Value; set => _fixedExposureProp.Value = value; }
		public float Compensation { get => _compensationProp.Value; set => _compensationProp.Value = value; }

		public event Action<SystemLanguage>? OnLanguageChanged;
		public IReadOnlyList<SystemLanguage> SupportedLanguages => _localization.SupportedLanguages;

		public RenderPipelineUtility.PipelineType PipelineType => (this.Generator != null) ? this.Generator.PipelineType : RenderPipelineUtility.PipelineType.Unsupported;

		EditorFpsCounter _fpsCounter = new();
		public int Fps => _fpsCounter.Fps;

		EditorDeltaTime _editorDeltaTime = new();
		public float DeltaTime => _editorDeltaTime.editorDeltaTimef;

		public U17CubemapGeneratorWindowContext()
		{
			_lastAssetPath = KeyValueStore.LoadString(SettingsKeys.LastAssetPath, _lastAssetPath);
			this.PreviewScene = new PreviewScene();
			this.PreviewScene.camera.transform.position = new Vector3(0f, 0f, -2);

			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPathCubemapGeneratorPrefab);
			if (prefab == null)
			{
				throw new InvalidOperationException($"{AssetPathCubemapGeneratorPrefab} not found");
			}
			var goGenerator = UnityEngine.Object.Instantiate(prefab);
			goGenerator.hideFlags = HideFlags.HideAndDontSave;
			this.Generator = goGenerator.GetComponent<U17CubemapGenerator>();
			this.Generator.SetEditorDragControl(true);
			this.Generator.OnPipelineChanged += StartRedraw;

			this.Generator.PreviewSphere.transform.SetParent(null);
			this.PreviewScene.AddGameObject(this.Generator.PreviewSphere);
			this.Generator.PreviewCube.transform.SetParent(null);
			this.PreviewScene.AddGameObject(this.Generator.PreviewCube);

			_languageProp = new PropertyStorableSystemLanguage(SettingsKeys.Language, onSetDefault: () => _localization.Language);
			_localization.Language = _languageProp.Value;
			_languageProp.OnValueChanged += (value) =>
			{
				_localization.Language = value;
				this.OnLanguageChanged?.Invoke(value);
			};

			_specificCameraProp.OnValueChanged += (_) => RequestRedraw();
			_previewObjectProp.OnValueChanged += (_) => UpdatePreviewObject();
			_inputSourceProp.OnValueChanged += (_) => RequestRedraw();
			_textureWidthProp.OnValueChanged += (_) => RequestRedraw();
			_cubemapProp.OnValueChanged += (_) => RequestRedraw();
			_textureLeftProp.OnValueChanged += (_) => RequestRedraw();
			_textureRightProp.OnValueChanged += (_) => RequestRedraw();
			_textureTopProp.OnValueChanged += (_) => RequestRedraw();
			_textureBottomProp.OnValueChanged += (_) => RequestRedraw();
			_textureFrontProp.OnValueChanged += (_) => RequestRedraw();
			_textureBackProp.OnValueChanged += (_) => RequestRedraw();
			_usingCameraAnglesProp.OnValueChanged += (_) => RequestRedraw();
			_horizontalRotationProp.OnValueChanged += (_) => RequestRedraw();
			_exposureOverrideProp.OnValueChanged += (_) => RequestRedraw();
			_fixedExposureProp.OnValueChanged += (_) => RequestRedraw();
			_compensationProp.OnValueChanged += (_) => RequestRedraw();

			this.Generator.SetPreviewObject(U17CubemapGenerator.PreviewObject.None);
			RequestRedraw();
			// UpdatePreviewObject() is called when redraw is completed by RequestRedraw
		}

		public void Dispose()
		{
			this.PreviewScene?.Dispose();
			this.PreviewScene = null!;
			if (this.Generator != null)
			{
				UnityEngine.Object.DestroyImmediate(this.Generator);
				this.Generator = null;
			}
		}

		public void OnUpdate()
		{
			_editorDeltaTime.OnUpdate();
			float deltaTime = _editorDeltaTime.editorDeltaTimef;

			if (this.Generator != null && !this.Generator.IsProcessing)
			{
				if (_requestRedraw)
				{
					_requestRedraw = false;
					StartRedraw();
				}

				UpdateAnimation(deltaTime);
			}


			_fpsCounter.OnUpdate(deltaTime);
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
			if (this.Generator == null) throw new InvalidOperationException();

			if (!CanRender())
			{
				ClearCubemap();
				return;
			}
#if USING_HDRP
			if (this.PipelineType == RenderPipelineUtility.PipelineType.HDPipeline)
			{
				this.Generator.SetExposureOverride(this.ExposureOverride, this.FixedExposure, this.Compensation);
			}
#endif

			switch (this.InputSource)
			{
				case U17CubemapGenerator.InputSource.CurrentScene:
					this.Generator.SetHorizontalRotation(_rotationAngleArray[(int)this.HorizontalRotation]);
					this.Generator.SetUsingCameraAngles(this.UsingCameraAngles);
					this.Generator.SetSpecificCamera(this.SpecificCamera);
					this.Generator.SetCubemapWidth(_textureWidthArray[(int)this.TextureWidth]);
					break;

				case U17CubemapGenerator.InputSource.Cubemap:
					this.Generator.SetSourceCubemap(this.Cubemap!);
					break;

				case U17CubemapGenerator.InputSource.SixSided:
					var textures = GetSixSidedTextures();
					for (int i = 0; i < textures.Length; i++)
					{
						this.Generator.SetSourceTexture((CubemapFace)i, textures[i]!);
					}
					break;
			}

			this.Generator.StartRender(this.InputSource, onCompleted: () =>
			{
				UpdatePreviewObject();
			});
		}

		void UpdatePreviewObject()
		{
			if (this.Generator == null) throw new InvalidOperationException();

			switch (_previewObjectProp.Value)
			{
				case PreviewObjectType.Sphere:
					this.Generator.SetPreviewObject(U17CubemapGenerator.PreviewObject.Sphere);
					break;
				case PreviewObjectType.Cube:
					this.Generator.SetPreviewObject(U17CubemapGenerator.PreviewObject.Cube);
					break;
			}
		}

		public void ResetPreviewRotation()
		{
			if (this.Generator == null) throw new InvalidOperationException();

			this.Generator.ResetPreviewCubeRotation();
		}

		public void ExportSaveCubemap()
		{
			if (this.Generator == null) throw new InvalidOperationException();

			this.Generator.SetFillMatcapOutsideByEdgeColor(this.FillMatcapOutsideByEdgeColor);
			this.Generator.SetOutputDesirableHDR(this.OutputHDR);
			this.Generator.SetOutputDesirableSRGB(this.OutputSRGB);
			this.Generator.SetOutputDesirableCubemap(this.OutputCubemap);
			this.Generator.SetOutputDesirableGenerateMipmap(this.OutputGenerateMipmap);
			this.Generator.SetCubemapOutputEquirectangularRotation(this.EquirectangularRotation);
			this.Generator.SetCubemapOutputLayout(this.OutputLayout);
			this.Generator.SaveAsset(_lastAssetPath, onCompleted: (path) =>
			{
				_lastAssetPath = path;
				KeyValueStore.SaveString(SettingsKeys.LastAssetPath, _lastAssetPath);
			});
		}

		public void ClearCubemap()
		{
			if (this.Generator == null) throw new InvalidOperationException();

			this.Generator.ClearCubemap();
		}

		Texture2D?[] GetSixSidedTextures()
		{
			return new Texture2D?[] { this.TextureLeft, this.TextureRight, this.TextureTop, this.TextureBottom, this.TextureFront, this.TextureBack };
		}

		public bool CanRender()
		{
			switch (this.InputSource)
			{
				case U17CubemapGenerator.InputSource.Cubemap:
					if (this.Cubemap == null)
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

	}
}
