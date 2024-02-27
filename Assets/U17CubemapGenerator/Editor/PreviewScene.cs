using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using UnityEngine.Rendering;
using System.Linq;
using System.IO;

namespace Uchuhikoshi.U17CubemapGenerator
{
	// original: https://light11.hatenadiary.com/entry/2020/02/29/211114

	public class PreviewScene : IDisposable
	{
		public Scene scene { get; private set; }
		public Camera camera { get; private set; }
		public RenderTexture renderTexture { get; private set; }
		public Vector2Int renderTextureSize { get; set; } = new Vector2Int(1024, 1024);

		SavedRenderSettings _savedRenderSettings;
		readonly List<GameObject> _gameObjects = new List<GameObject>();
		bool _didInitialize = false;

		public PreviewScene(string environmentSceneAssetPath = null)
		{
			try {
				this.scene = EditorSceneManager.NewPreviewScene();
        
				if (environmentSceneAssetPath != null) {
					_savedRenderSettings = SavedRenderSettings.Create(environmentSceneAssetPath);
					CopyRootGameObjects(environmentSceneAssetPath);
				}

				// Deactivate unused cameras
				var oldCameras = this.scene
					.GetRootGameObjects()
					.SelectMany(x => x.GetComponentsInChildren<Camera>());
				foreach (var oldCamera in oldCameras) {
					oldCamera.enabled = false;
				}

				var cameraGO = new GameObject("Preview Scene Camera", typeof(Camera));
				cameraGO.transform.position = new Vector3(0, 0, -10);
				AddGameObject(cameraGO);
				this.camera = cameraGO.GetComponent<Camera>();
				this.camera.cameraType = CameraType.Preview;
				this.camera.forceIntoRenderTexture = true;
				this.camera.scene = this.scene;
				this.camera.enabled = false; // Deactivate so as not to affect GameView
        
				var hasDirectionalLight = this.scene
					.GetRootGameObjects()
					.SelectMany(x => x.GetComponentsInChildren<Light>())
					.Any(x => x.type == LightType.Directional);
				if (!hasDirectionalLight) {
					var lightGO = new GameObject("Directional Light", typeof(Light));
					AddGameObject(lightGO);
					lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
					var light = lightGO.GetComponent<Light>();
					light.type = LightType.Directional;
				}
            
				_didInitialize = true;
			}
			catch (Exception e) {
				Dispose();
				_didInitialize = false;
				throw e;
			}
		}

		public void Render(bool useScriptableRenderPipeline = false)
		{
			if (!_didInitialize) {
				return;
			}
			// Change RenderSettings
			if (_savedRenderSettings != null && Unsupported.SetOverrideLightingSettings(this.scene)) {
				_savedRenderSettings.Apply();
			}
        
			// Create RenderTexture if needed
			if (!this.renderTexture || this.renderTexture.width != this.renderTextureSize.x || this.renderTexture.height != this.renderTextureSize.y)
			{
				if (this.renderTexture)
				{
					Object.DestroyImmediate(this.renderTexture);
					this.renderTexture = null;
				}

				var format = this.camera.allowHDR ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R8G8B8A8_UNorm;
				this.renderTexture = new RenderTexture(this.renderTextureSize.x, this.renderTextureSize.y, 32, format);
			}
			this.camera.targetTexture = this.renderTexture;
        
			// Render
			var oldAllowPipes = Unsupported.useScriptableRenderPipeline;
			Unsupported.useScriptableRenderPipeline = useScriptableRenderPipeline;
			this.camera.Render();
			Unsupported.useScriptableRenderPipeline = oldAllowPipes;

			this.camera.targetTexture = null;
			// Restore RenderSettings
			if (_savedRenderSettings != null) {
				Unsupported.RestoreOverrideLightingSettings();
			}
		}

		public void Dispose()
		{
	//        Camera.targetTexture = null;

			if (this.renderTexture != null){
				Object.DestroyImmediate(this.renderTexture);
				this.renderTexture = null;
			}

			foreach (var go in _gameObjects){
				Object.DestroyImmediate(go);
			}
			_gameObjects.Clear();

			EditorSceneManager.ClosePreviewScene(this.scene);
		}
    
		/// <summary>
		/// Add GameObject to preview scene
		/// </summary>
		public void AddGameObject(GameObject go)
		{
			if (_gameObjects.Contains(go)){
				return;
			}
			SceneManager.MoveGameObjectToScene(go, this.scene);
			_gameObjects.Add(go);
		}

		/// <summary>
		/// Add prefab instance to preview scene
		/// </summary>
		public GameObject InstantiatePrefab(GameObject prefab)
		{
			var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, this.scene);
			_gameObjects.Add(instance);
			return instance;
		}

		/// <summary>
		/// Copy all root GameObjects of source scene to preview scene
		/// </summary>
		void CopyRootGameObjects(string sourceSceneAssetPath)
		{
			GameObject[] rootGameObjects = null;
			for (int i = 0; i < EditorSceneManager.sceneCount; i++) {
				var scene = EditorSceneManager.GetSceneAt(i);
				if (scene.path == sourceSceneAssetPath) {
					rootGameObjects = scene.GetRootGameObjects();
					break;
				}
			}
			if (rootGameObjects == null) {
				var scene = EditorSceneManager.OpenScene(sourceSceneAssetPath, OpenSceneMode.Additive);
				rootGameObjects = scene.GetRootGameObjects();
				EditorSceneManager.CloseScene(scene, true);
			}
			if (rootGameObjects != null) {
				foreach (var rootGameObject in rootGameObjects) {
					AddGameObject(GameObject.Instantiate(rootGameObject));
				}
			}
		}

		public class SavedRenderSettings
		{
			Material _skybox;
			Light _sun;
			AmbientMode _ambientMode;
			SphericalHarmonicsL2 _ambientProbe;
			Color _ambientSkyColor;
			Color _ambientEquatorColor;
			Color _ambientGroundColor;
			Color _ambientLight;
			float _ambientIntensity;
			DefaultReflectionMode _defaultReflectionMode;
			int _defaultReflectionResolution;
			Texture _customReflectionTexture;
			float _reflectionIntensity;
			int _reflectionBounces;
			Color _substractiveShadowColor;
			bool _fog;
			FogMode _fogMode;
			Color _fogColor;
			float _fogDensity;
			float _fogStartDistance;
			float _fogEndDistance;
			float _flareFadeSpeed;
			float _flareStrength;
			float _haloStrength;

			public static SavedRenderSettings Create(string scenePath)
			{
				var result = new SavedRenderSettings();
				Scene? oldActiveScene = null;
				if (scenePath != EditorSceneManager.GetActiveScene().path) {
					oldActiveScene = EditorSceneManager.GetActiveScene();
					var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
					EditorSceneManager.SetActiveScene(scene);
				}
            
				result._skybox = RenderSettings.skybox;
				result._sun = RenderSettings.sun;
				result._ambientMode = RenderSettings.ambientMode;
				result._ambientProbe = RenderSettings.ambientProbe;
				result._ambientSkyColor = RenderSettings.ambientSkyColor;
				result._ambientEquatorColor = RenderSettings.ambientEquatorColor;
				result._ambientGroundColor = RenderSettings.ambientGroundColor;
				result._ambientLight = RenderSettings.ambientLight;
				result._ambientIntensity = RenderSettings.ambientIntensity;
				result._defaultReflectionMode = RenderSettings.defaultReflectionMode;
				result._defaultReflectionResolution = RenderSettings.defaultReflectionResolution;
#if UNITY_2022_1_OR_NEWER
				result._customReflectionTexture = RenderSettings.customReflectionTexture;
#endif
				// If defaultReflectionMode is Skybox, search and set the created cube map
				if (result._defaultReflectionMode == DefaultReflectionMode.Skybox && Lightmapping.lightingDataAsset != null) {
					var lightingDataAssetPath = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
					var lightingDataAssetDirectoryName = Path.GetDirectoryName(lightingDataAssetPath);
					var environmentProbeAssetPath = Directory
						.GetFiles(lightingDataAssetDirectoryName)
						.FirstOrDefault(x => x.EndsWith(".exr"));
					if (!string.IsNullOrEmpty(environmentProbeAssetPath)) {
						result._defaultReflectionMode = DefaultReflectionMode.Custom;
						result._customReflectionTexture = AssetDatabase.LoadAssetAtPath<Texture>(environmentProbeAssetPath.Replace("\\", "/"));
					}
				}
				result._reflectionIntensity = RenderSettings.reflectionIntensity;
				result._reflectionBounces = RenderSettings.reflectionBounces;
				result._substractiveShadowColor = RenderSettings.subtractiveShadowColor;
				result._fog = RenderSettings.fog;
				result._fogMode = RenderSettings.fogMode;
				result._fogColor = RenderSettings.fogColor;
				result._fogDensity = RenderSettings.fogDensity;
				result._fogStartDistance = RenderSettings.fogStartDistance;
				result._fogEndDistance = RenderSettings.fogEndDistance;
				result._flareFadeSpeed = RenderSettings.flareFadeSpeed;
				result._flareStrength = RenderSettings.flareStrength;
				result._haloStrength = RenderSettings.haloStrength;

				if (oldActiveScene.HasValue) {
					var scene = EditorSceneManager.GetActiveScene();
					EditorSceneManager.SetActiveScene(oldActiveScene.Value);
					EditorSceneManager.CloseScene(scene, true);
				}
				return result;
			}

			public void Apply()
			{
				RenderSettings.skybox = _skybox;
				RenderSettings.sun = _sun;
				RenderSettings.ambientMode = _ambientMode;
				RenderSettings.ambientProbe = _ambientProbe;
				RenderSettings.ambientSkyColor = _ambientSkyColor;
				RenderSettings.ambientEquatorColor = _ambientEquatorColor;
				RenderSettings.ambientGroundColor = _ambientGroundColor;
				RenderSettings.ambientLight = _ambientLight;
				RenderSettings.ambientIntensity = _ambientIntensity;
				RenderSettings.defaultReflectionMode = _defaultReflectionMode;
				RenderSettings.defaultReflectionResolution = _defaultReflectionResolution;
#if UNITY_2022_1_OR_NEWER
				RenderSettings.customReflectionTexture = _customReflectionTexture;
#endif
				RenderSettings.reflectionIntensity = _reflectionIntensity;
				RenderSettings.reflectionBounces = _reflectionBounces;
				RenderSettings.subtractiveShadowColor = _substractiveShadowColor;
				RenderSettings.fog = _fog;
				RenderSettings.fogMode = _fogMode;
				RenderSettings.fogColor = _fogColor;
				RenderSettings.fogDensity = _fogDensity;
				RenderSettings.fogStartDistance = _fogStartDistance;
				RenderSettings.fogEndDistance = _fogEndDistance;
				RenderSettings.flareFadeSpeed = _flareFadeSpeed;
				RenderSettings.flareStrength = _flareStrength;
				RenderSettings.haloStrength = _haloStrength;
			}
		}
	}
}
