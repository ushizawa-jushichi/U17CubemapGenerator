using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	[ExecuteAlways]
	public partial class U17CubemapGenerator : MonoBehaviour, IU17CubemapGenerator
	{
		[SerializeField] Camera _rendererCamera = null!;
		[SerializeField] int _textureWidth = 1024;
		[SerializeField] float _equirectangularRotationEulerY = 0;
		[SerializeField] GameObject _previewRoot = null!;
		[SerializeField] GameObject _previewSphere = null!;
		[SerializeField] GameObject _previewCube = null!;
		[SerializeField] GameObject _previewSphereMatcap = null!;

		public enum InputSource
		{
			CurrentScene,
			SixSided,
			Cubemap,
		}

		public enum OutputLayout
		{
			CrossHorizontal,
			CrossVertical,
			StraitHorizontal,
			StraitVertical,
			SixSided,
			Equirectanglar,
			Matcap,
		}

		public enum PreviewObject
		{
			None,
			Sphere,
			Cube,
		}

		public enum InternalPreviewObjectMode
		{
			Default,
			HideAll,
			Matcap,
		}

		public const bool ExposureOverrideEnabledDefault = true;
		public const float FixedExposureStandard = 12f;

		RenderPipelineUtility.PipelineType _pipelineType = RenderPipelineUtility.PipelineType.Unsupported;
		Shader? _shaderBlitter = null!;
		Shader? _shaderPreview = null!;
		Material? _materialBlitter = null!;
		RenderTexture? _cubemapRT;
		Cubemap? _cubemapAlter;
		readonly Texture2D?[] _cachedFaces = new Texture2D[6];
		bool _isRenderProcessing;
		bool _isDone;
		bool _isSourceHDR;
		bool _usingCameraAngles;
		bool _isOutputDesirableHDR;
		bool _isOutputDesirableSRGB;
		bool _isOutputDesirableCubemap;
		bool _isOutputDesirableGenerateMipmap;
		bool _isOutputHDR;
		bool _isOutputSRGB;
		bool _isOutputCubemap;
		bool _isOutputGenerateMipmap;
		bool _fillMatcapOutsideByEdgeColor;
		bool _isPipelineChanging;
		bool _activeSceneChanged;
		readonly Texture2D?[] _6sidedSources = new Texture2D[6];
		Camera?	_specificCamera;
		readonly List<Material> _previewMeshMaterials = new List<Material>();
		Quaternion _rotationBase = Quaternion.identity;
		float _horizontalRotation = 0f;
		bool _exposureOverride = ExposureOverrideEnabledDefault;
		float _fixedExposure = FixedExposureStandard;
		float _compensation = 0f;

		Matrix4x4 _previewRotationMatrix = Matrix4x4.identity;

		static readonly int _idMainTex = Shader.PropertyToID("_MainTex");
		static readonly int _idCubeTex = Shader.PropertyToID("_CubeTex");
		static readonly int _idPreviewRotationMatrix = Shader.PropertyToID("_PreviewRotationMatrix");
		static readonly int _idRotationY = Shader.PropertyToID("_RotationY");

		InputSource _inputSource = InputSource.CurrentScene;
		OutputLayout _outputLayout = OutputLayout.CrossHorizontal;
		PreviewObject _previewObject = PreviewObject.Sphere;

		bool _started;
		Action? _onStartExit;
		Action? _onUpdate;

		public bool IsSourceHDR => _isSourceHDR;
		public RenderPipelineUtility.PipelineType PipelineType => _pipelineType;
		public Action? OnPipelineChanged { get; set; }

#if UNITY_EDITOR
		public GameObject PreviewSphere => _previewSphere;
		public GameObject PreviewCube => _previewCube;
#endif
		public bool IsPipelineChanging => _isPipelineChanging;
		public bool IsProcessing => _isRenderProcessing
#if UNITY_EDITOR
			|| _isSaveProcessing
#endif
			;

		void Start()
		{
			Setup();
			_started = true;
			_onStartExit?.Invoke();

#if UNITY_EDITOR
#if UNITY_2021_1_OR_NEWER
			RenderPipelineManager.activeRenderPipelineTypeChanged -= OnActivePipelineChanged;
			RenderPipelineManager.activeRenderPipelineTypeChanged += OnActivePipelineChanged;
#endif
			EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged; 
			EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
#endif
			SceneManager.activeSceneChanged -= OnActiveSceneChanged;
			SceneManager.activeSceneChanged += OnActiveSceneChanged;
		}

		void OnDestroy()
		{
#if USING_HDRP
			CleanupExposureOverride();
#endif
#if UNITY_EDITOR
#if UNITY_2021_1_OR_NEWER
			RenderPipelineManager.activeRenderPipelineTypeChanged -= OnActivePipelineChanged;
#endif
			EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
#endif
			SceneManager.activeSceneChanged -= OnActiveSceneChanged;
			CleanupRenderCurrentScene();
			DisposeRenderCache();
			DisposeMaterials();
			_specificCamera = null;
			for (int i = 0; i < _6sidedSources.Length; i++)
			{
				_6sidedSources[i] = null;
			}
			_onStartExit = null;
			_onUpdate = null;
		}

		void Update()
		{
			if (_isPipelineChanging)
			{
				return;
			}
			if (_activeSceneChanged)
			{
				_activeSceneChanged = false;
				DisposeRenderCache();
				DisposeMaterials();
				Setup();
				StartRender(_inputSource);
			}
			if (!this.IsProcessing)
			{
				var pipelineType = RenderPipelineUtility.DetectPipeline();
				if (_pipelineType != pipelineType)
				{
					_pipelineType = pipelineType;
					OnPipelineChanging();
					return;
				}
				OnUpdateDragRotate();
			}
			_onUpdate?.Invoke();
		}

		public static void DestroyUnityObject(UnityEngine.Object obj) // Same as CoreUtils.Destroy
		{
			if (obj != null)
			{
#if UNITY_EDITOR
				if (Application.isPlaying && !UnityEditor.EditorApplication.isPaused)
					UnityEngine.Object.Destroy(obj);
				else
					UnityEngine.Object.DestroyImmediate(obj);
#else
				UnityEngine.Object.Destroy(obj);
#endif
			}
		}

		void Setup()
		{
			Shader? FindShader(string shaderName)
			{
				var shader = Shader.Find(shaderName);
				if (shader == null) { throw new InvalidOperationException($"{shaderName} load failed"); }
				return shader;
			}

			_pipelineType = RenderPipelineUtility.DetectPipeline();
			switch (_pipelineType)
			{
#if USING_URP 
				case RenderPipelineUtility.PipelineType.UniversalPipeline:
					_shaderBlitter = FindShader("Uchuhikoshi/U17CubemapGenerator/BlitterURP");
					_shaderPreview = FindShader("Uchuhikoshi/U17CubemapGenerator/PreviewURP");
					break;
#endif
#if USING_HDRP 
				case RenderPipelineUtility.PipelineType.HDPipeline:
					_shaderBlitter = FindShader("Uchuhikoshi/U17CubemapGenerator/BlitterHDRP");
					_shaderPreview = FindShader("Uchuhikoshi/U17CubemapGenerator/PreviewHDRP");
					break;
#endif
				case RenderPipelineUtility.PipelineType.BuiltInPipeline:
					_shaderBlitter = FindShader("Uchuhikoshi/U17CubemapGenerator/BlitterBuiltin");
					_shaderPreview = FindShader("Uchuhikoshi/U17CubemapGenerator/PreviewBuiltin");
					break;
				default: throw new InvalidOperationException("Unsupported");
			}

			// Preview Mesh
			var goes = new GameObject[] { _previewCube, _previewSphere, _previewSphereMatcap };
			for (int i = 0; i < goes.Length; i++)
			{
				var meshRenderer = goes[i].GetComponent<MeshRenderer>();
				var material = new Material(_shaderPreview);
				_previewMeshMaterials.Add(material);
				meshRenderer.material = material;
			}
			_previewCube.GetComponent<MeshRenderer>().sharedMaterial.EnableKeyword("SKYBOX_ON");

			UpdatePreviewMeshMatrix();
			UpdatePreviewMeshTexture(null);
#if UNITY_EDITOR
			GameObjectUtility.RemoveMonoBehavioursWithMissingScript(_rendererCamera.gameObject);
#endif
		}

		void OnActiveSceneChanged(Scene current, Scene next)
		{
			_onUpdate = null;
			_activeSceneChanged = true;
		}

		void OnPipelineChanging()
		{
			_onUpdate = null;
			DisposeRenderCache();
			DisposeMaterials();
			_isPipelineChanging = true;
		}

		void OnActivePipelineChanged()
		{
			DisposeRenderCache();
			DisposeMaterials();
			Setup();
			_isPipelineChanging = false;
			this.OnPipelineChanged?.Invoke();
		}

		void DisposeMaterials()
		{
			if (_materialBlitter != null)
			{
				DestroyUnityObject(_materialBlitter);
			}

			for (int i = 0; i < _previewMeshMaterials.Count; i++)
			{
				DestroyUnityObject(_previewMeshMaterials[i]);
			}
			_previewMeshMaterials.Clear();
		}

		void DisposeRenderCache()
		{
			DisposeCachedFaces();

			if (_cubemapRT != null)
			{
				_cubemapRT.Release();
				_cubemapRT = null;
			}
			if (_cubemapAlter != null)
			{
				DestroyImmediate(_cubemapAlter);
				_cubemapAlter = null;
			}
		}

		public void ClearCubemap()
		{
			DisposeRenderCache();
		}

		void DisposeCachedFaces()
		{
			for (int i = 0; i < 6; i++)
			{
				if (_cachedFaces[i] != null)
				{
					DestroyImmediate(_cachedFaces[i]);
					_cachedFaces[i] = null;
				}
			}
		}

		public void SetHorizontalRotation(float horizontalRotation)
		{
			_horizontalRotation = horizontalRotation;
		}

		public void SetUsingCameraAngles(bool usingCameraAngles)
		{
			_usingCameraAngles = usingCameraAngles;
		}

		public void SetSpecificCamera(Camera? specificCamera)
		{
			_specificCamera = specificCamera;
		}

		public void SetCubemapWidth(int width)
		{
			if (this.IsProcessing) { throw new InvalidOperationException("Now processing"); }
			_textureWidth = width;
		}

		public void SetExposureOverride(bool exposureOverride, float fixelExposure, float compensatipn)
		{
			if (this.IsProcessing) { throw new InvalidOperationException("Now processing"); }
			_exposureOverride = exposureOverride;
			_fixedExposure = fixelExposure;
			_compensation = compensatipn;
		}

#if UNITY_EDITOR
		public void SetFillMatcapOutsideByEdgeColor(bool value)
		{
			if (this.IsProcessing) { throw new InvalidOperationException("Now processing"); }
			_fillMatcapOutsideByEdgeColor = value;
		}

		public void SetOutputDesirableHDR(bool value)
		{
			if (this.IsProcessing) { throw new InvalidOperationException("Now processing"); }
			_isOutputDesirableHDR = value;
		}

		public void SetOutputDesirableSRGB(bool value)
		{
			if (this.IsProcessing) { throw new InvalidOperationException("Now processing"); }
			_isOutputDesirableSRGB = value;
		}

		public void SetOutputDesirableCubemap(bool value)
		{
			if (this.IsProcessing) { throw new InvalidOperationException("Now processing"); }
			_isOutputDesirableCubemap = value;
		}

		public void SetOutputDesirableGenerateMipmap(bool value)
		{
			if (this.IsProcessing) { throw new InvalidOperationException("Now processing"); }
			_isOutputDesirableGenerateMipmap = value;
		}

		public void SetCubemapOutputLayout(OutputLayout cubemapOutputLayout)
		{
			if (this.IsProcessing) { throw new InvalidOperationException("Now processing"); }
			_outputLayout = cubemapOutputLayout;
		}
#endif

		public void StartRender(InputSource inputSource, Action? onCompleted = null)
		{
			if (this.IsProcessing) { throw new InvalidOperationException("Now processing"); }
			_isRenderProcessing = true;
			_inputSource = inputSource;

			if (!_started)
			{
				_onStartExit += () => 
				{
					StartCoroutine(RenderProcessCoroutine(onCompleted));
				};
			}
			else
			{
				StartCoroutine(RenderProcessCoroutine(onCompleted));
			}
		}

		IEnumerator RenderProcessCoroutine(Action? onCompleted)
		{
			try
			{
				yield return StartCoroutine(RenderProcessMainCoroutine(onCompleted));
			}
			finally
			{
				CleanupRenderCurrentScene();
				_isRenderProcessing = false;
				if (_rendererCamera != null)
				{
					_rendererCamera.gameObject.SetActive(false);
				}
			}
		}

		IEnumerator RenderProcessMainCoroutine(Action? onCompleted)
		{
			int cubemapWidth = 0;
			switch (_inputSource)
			{
				case InputSource.CurrentScene:
					VerifyAndSetupRenderCurrentScene(out cubemapWidth);
					break;

				case InputSource.SixSided:
					VerifyAndSetupLoad6Sided(out cubemapWidth);
					break;

				case InputSource.Cubemap:
					VerifyAndSetupLoadCubemap();
					break;

				default:
					throw new InvalidOperationException("Unsupported InputSource");
			}

			if (_shaderBlitter == null) { throw new InvalidOperationException($"{nameof(_shaderBlitter)} null"); }

			if (_materialBlitter == null)
			{
				_materialBlitter = new Material(_shaderBlitter);
				if (_materialBlitter == null) { throw new InvalidOperationException($"{nameof(_materialBlitter)} null"); }
			}

			ClearCubemap();

			if (_inputSource == InputSource.Cubemap)
			{
				LoadCubemapFaces();
				UpdatePreviewMeshTexture(_texCubemap);
				_isDone = true;
				_isRenderProcessing = false;
				onCompleted?.Invoke();
				yield break;
			}

			// For HDRP-6Sided. Low Speedw.
			// I really want to make the process similar to the subsequent URP version.(2023/06/04)
#if USING_HDRP 
			if ((_pipelineType == RenderPipelineUtility.PipelineType.HDPipeline) &&
				(_inputSource == InputSource.SixSided))
			{
				LoadCubemapFacesFrom6Sided(cubemapWidth);
				UpdatePreviewMeshTexture(_cubemapAlter);
				_isDone = true;
				_isRenderProcessing = false;
				onCompleted?.Invoke();
				yield break;
			}
#endif

			var targetSourceCamera = GetTargetCamera();
			_rendererCamera.CopyFrom(targetSourceCamera);
			_rendererCamera.gameObject.SetActive(true);

			Camera? camera = _rendererCamera;
			_isSourceHDR = camera.allowHDR;
			_rotationBase = _usingCameraAngles ? targetSourceCamera.transform.rotation : Quaternion.identity;
			_rotationBase = Quaternion.Euler(0f, _horizontalRotation, 0f) * _rotationBase;

			var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32;
			var cubemapRTDesc = new RenderTextureDescriptor(cubemapWidth, cubemapWidth, format)
			{
				dimension = UnityEngine.Rendering.TextureDimension.Cube,
				enableRandomWrite = true
			};
			_cubemapRT = new RenderTexture(cubemapRTDesc);
			UpdatePreviewMeshTexture(null);

#if USING_HDRP
			if (_exposureOverride && (_pipelineType == RenderPipelineUtility.PipelineType.HDPipeline))
			{
				StartExposureOverride(_fixedExposure, _compensation);
			}
#endif

			_isDone = false;
			UpdatePreviewObjectVisibility(InternalPreviewObjectMode.HideAll);

			switch (_inputSource)
			{
				case InputSource.CurrentScene:
					RenderCurrentScene();
					break;
				case InputSource.SixSided:
					Load6Sided();
					break;
			}

			yield return new WaitUntil(() => _isDone);

			UpdatePreviewMeshTexture(_cubemapRT);
#if USING_HDRP
			CleanupExposureOverride();
#endif

			_isRenderProcessing = false;
			onCompleted?.Invoke();
			UpdatePreviewObjectVisibility(InternalPreviewObjectMode.Default);
		}

		public void SetPreviewObject(PreviewObject previewObject)
		{
			_previewObject = previewObject;
			if (this.IsProcessing)
			{
				return;
			}
			UpdatePreviewObjectVisibility(InternalPreviewObjectMode.Default);
		}

		void UpdatePreviewObjectVisibility(InternalPreviewObjectMode internalPreviewObjectMode)
		{
			switch (internalPreviewObjectMode)
			{
				case InternalPreviewObjectMode.Default:
					_previewRoot.SetActive(_previewObject != PreviewObject.None);
					_previewSphere.SetActive(_previewObject == PreviewObject.Sphere);
					_previewCube.SetActive(_previewObject == PreviewObject.Cube);
					_previewSphereMatcap.SetActive(false);
					break;
				case InternalPreviewObjectMode.HideAll:
					_previewRoot.SetActive(false);
					break;
				case InternalPreviewObjectMode.Matcap:
					_previewRoot.SetActive(true);
					_previewSphere.SetActive(false);
					_previewCube.SetActive(false);
					_previewSphereMatcap.SetActive(true);
					break;
			}
		}

		Camera GetTargetCamera()
		{ 
			if (_specificCamera != null)
			{ 
				return _specificCamera;	
			}
			if (Camera.main == null)
			{
				var scene = SceneManager.GetActiveScene();
				if (scene != null)
				{
					var rootObjects = scene.GetRootGameObjects();
					foreach (var obj in rootObjects)
					{
						var camera = obj.GetComponentInChildren<Camera>();
						if (camera != null)
						{
							return camera;
						}
					}
				}
				throw new InvalidOperationException("No Camera");
			}
			return Camera.main;
		}

		void UpdatePreviewMeshTexture(Texture? cubemapTex)
		{
			for (int i = 0; i < _previewMeshMaterials.Count; i++) 
			{
				_previewMeshMaterials[i].SetTexture(_idMainTex, cubemapTex);
			}
		}

		void UpdatePreviewMeshMatrix()
		{
			for (int i = 0; i < _previewMeshMaterials.Count; i++)
			{
				_previewMeshMaterials[i].SetMatrix(_idPreviewRotationMatrix, _previewRotationMatrix);
			}
		}

		public static bool IsOutputLayoutAvailableBeCubemap(OutputLayout outputLayout)
		{
			switch (outputLayout)
			{
				case OutputLayout.CrossHorizontal:
				case OutputLayout.CrossVertical:
				case OutputLayout.StraitHorizontal:
				case OutputLayout.StraitVertical:
				case OutputLayout.Equirectanglar:
					return true;
			}
			return false;
		}

		RenderTextureDescriptor GetRenderTextureDescriptorForOutputTemporary(int width, int height, bool depth)
		{
			var format = _rendererCamera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32;
			var desc = new RenderTextureDescriptor(width, height, format, depth ? 32 : 0);
			desc.sRGB = _isOutputSRGB;
			desc.enableRandomWrite = true;
			return desc;
		}

		Texture2D CreateTexture2DForOutputTemporary(int width, int height)
		{
			TextureFormat textureFormat = _isOutputHDR ? TextureFormat.RGBAHalf : TextureFormat.RGB24;
			Texture2D tex = new Texture2D(width, height, textureFormat, mipCount: -1, linear: !_isOutputSRGB);
			return tex;
		}
	}
}
