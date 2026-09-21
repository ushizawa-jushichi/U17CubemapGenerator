using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     エディター拡張用の独立したプレビューシーンとカメラ、RenderTexture のライフサイクルを管理するユーティリティ。
    /// </summary>
    public class PreviewScene : IDisposable
    {
        private const float DefaultCameraFieldOfView = 90f;
        private static readonly Vector3 DefaultCameraPosition = new(0, 0, -10);
        private static readonly Quaternion DefaultLightRotation = Quaternion.Euler(50, -30, 0);
        private static readonly Vector2Int DefaultRenderTextureSize = new(1024, 1024);

        private readonly bool _didInitialize;

        private readonly List<GameObject> _gameObjects = new();

        public PreviewScene()
        {
            try
            {
                Scene = EditorSceneManager.NewPreviewScene();

                var cameraGo = CreatePreviewGameObject("Preview Scene Camera", typeof(Camera));
                cameraGo.transform.position = DefaultCameraPosition;
                Camera = cameraGo.GetComponent<Camera>();
                Camera.cameraType = CameraType.Preview;
                Camera.fieldOfView = DefaultCameraFieldOfView;
                Camera.nearClipPlane = 0.01f;
                Camera.forceIntoRenderTexture = true;
                Camera.scene = Scene;
                Camera.enabled = false; // Deactivate so as not to affect GameView

                var additionalData = Camera.GetUniversalAdditionalCameraData();
                additionalData.renderShadows = false;
                additionalData.renderPostProcessing = false;

                var lightGo = CreatePreviewGameObject("Directional Light", typeof(Light));
                lightGo.transform.rotation = DefaultLightRotation;
                var light = lightGo.GetComponent<Light>();
                light.type = LightType.Directional;

                _didInitialize = true;
            }
            catch (Exception)
            {
                Dispose();
                _didInitialize = false;
                throw;
            }
        }

        public Scene Scene { get; private set; }
        public Camera Camera { get; private set; } = null!;
        public RenderTexture RenderTexture { get; private set; } = null!;
        public Vector2Int RenderTextureSize { get; set; } = DefaultRenderTextureSize;

        public bool IsValid => _didInitialize && Scene.IsValid() && Camera != null;

        public void Dispose()
        {
            if (RenderTexture != null)
            {
                RenderTexture.Release();
                Object.DestroyImmediate(RenderTexture);
                RenderTexture = null!;
            }

            foreach (var go in _gameObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _gameObjects.Clear();
            Camera = null!;

            if (Scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(Scene);
                Scene = default;
            }
        }

        public void Render()
        {
            if (!_didInitialize || RenderTextureSize.x <= 0 || RenderTextureSize.y <= 0)
            {
                return;
            }

            var format = Camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32;

            if (RenderTexture == null ||
                RenderTexture.width != RenderTextureSize.x ||
                RenderTexture.height != RenderTextureSize.y ||
                RenderTexture.format != format)
            {
                if (RenderTexture != null)
                {
                    RenderTexture.Release();
                    Object.DestroyImmediate(RenderTexture);
                    RenderTexture = null!;
                }

                RenderTexture = new RenderTexture(RenderTextureSize.x, RenderTextureSize.y, 24, format);
                RenderTexture.hideFlags = HideFlags.HideAndDontSave;
                RenderTexture.name = $"U17CubeGen.PreviewScene_{RenderTexture.width}x{RenderTexture.height}";
            }

            Camera.aspect = (float)RenderTextureSize.x / RenderTextureSize.y;
            Camera.targetTexture = RenderTexture;

            try
            {
                if (CubemapRenderUtility.IsUniversalRenderPipelineActive())
                {
                    var request = new UniversalRenderPipeline.SingleCameraRequest
                    {
                        destination = RenderTexture
                    };

                    if (RenderPipeline.SupportsRenderRequest(Camera, request))
                    {
                        RenderPipeline.SubmitRenderRequest(Camera, request);
                    }
                    else
                    {
                        Camera.Render();
                    }
                }
                else
                {
                    Camera.Render();
                }
            }
            finally
            {
                Camera.targetTexture = null;
            }
        }

        private GameObject CreatePreviewGameObject(string name, params Type[] components)
        {
            var go = new GameObject(name, components);
            _gameObjects.Add(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            if (Scene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(go, Scene);
            }

            return go;
        }

        /// <summary>
        ///     Add GameObject to preview scene (owned and destroyed by PreviewScene)
        /// </summary>
        public void AddGameObject(GameObject go)
        {
            if (_gameObjects.Contains(go))
            {
                return;
            }

            if (go.transform.parent != null)
            {
                go.transform.SetParent(null);
            }

            if (Scene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(go, Scene);
            }

            _gameObjects.Add(go);
        }
    }
}
