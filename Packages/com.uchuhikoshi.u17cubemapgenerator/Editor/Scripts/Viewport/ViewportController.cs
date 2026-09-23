using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     プレビュー表示画面（Viewport）の描画、サイズ同期、3D プレビューオブジェクトの配置、ドラッグ操作による視点・オブジェクト回転、背景テクスチャへの反映を管理するコントローラー。
    /// </summary>
    public sealed class ViewportController : IDisposable
    {
        private const float SkyboxCameraFieldOfView = 90f;
        private const float ObjectCameraFieldOfView = 60f;

        private static readonly Vector3 SpherePreviewScale = new(2f, 2f, 2f);
        private static readonly Vector3 CubePreviewScale = new(1.3f, 1.3f, 1.3f);
        private static readonly Vector3 PreviewCameraPosition = new(0f, 0f, -2.036f);

        private static readonly int IDRotation = Shader.PropertyToID("_Rotation");
        private static readonly int IDPreviewRotationMatrix = Shader.PropertyToID("_PreviewRotationMatrix");

        private readonly VisualElement _bgContainer;
        private readonly DragEventHandler _dragEventHandler;
        private readonly EditorState _editorState;
        private readonly PreviewMaterials _materials;
        private readonly PreviewScene _previewScene;
        private readonly Settings _settings;
        private readonly float _toolbarHeight;

        private GameObject _previewCube = null!;
        private Matrix4x4 _previewRotationMatrix = Matrix4x4.identity;
        private GameObject _previewSphere = null!;
        private Vector2 _previousDragPosition;
        private Skybox _skybox = null!;

        private bool _hasCubemapBeenApplied;

#if UNITY_6000_2_OR_NEWER
        private EntityId _renderTextureEntityId;
#else
        private int _renderTextureInstanceID;
#endif

        public event Action<Vector3>? OnRotationChanged;

        public ViewportController(
            VisualElement bgContainer,
            DragEventHandler dragEventHandler,
            EditorState editorState,
            Settings settings,
            PreviewMaterials materials,
            float toolbarHeight)
        {
            _bgContainer = bgContainer;
            _dragEventHandler = dragEventHandler;
            _editorState = editorState;
            _settings = settings;
            _materials = materials;
            _toolbarHeight = toolbarHeight;
            DragSpeed = settings.DragSpeed;

            Assert.IsNotNull(_bgContainer, $"{nameof(_bgContainer)} is null.");
            Assert.IsNotNull(_dragEventHandler, $"{nameof(_dragEventHandler)} is null.");
            Assert.IsNotNull(_editorState, $"{nameof(_editorState)} is null.");
            Assert.IsNotNull(_settings, $"{nameof(_settings)} is null.");
            Assert.IsNotNull(_materials, $"{nameof(_materials)} is null.");

            _previewScene = new PreviewScene();

            ConfigurePreviewScene();

            _dragEventHandler.OnPointerDownEvent += OnPointerDown;
            _dragEventHandler.OnPointerMoveEvent += OnPointerMove;
        }

        public float DragSpeed { get; set; } = 1f;

        public Quaternion PreviewRotation { get; set; } = Quaternion.identity;

        public bool IsDragging => _dragEventHandler.IsDragging;

        public void Dispose()
        {
            _dragEventHandler.OnPointerDownEvent -= OnPointerDown;
            _dragEventHandler.OnPointerMoveEvent -= OnPointerMove;

            if (_bgContainer != null)
            {
                _bgContainer.style.backgroundImage = StyleKeyword.None;
#if UNITY_6000_2_OR_NEWER
                _renderTextureEntityId = default;
#else
                _renderTextureInstanceID = 0;
#endif
            }

            _previewScene?.Dispose();
            _previewSphere = null!;
            _previewCube = null!;

            OnRotationChanged = null;
        }

        private void ConfigurePreviewScene()
        {
            if (_previewScene.Camera == null || _materials.MatSkybox == null)
            {
                return;
            }

            _previewScene.Camera.transform.position = PreviewCameraPosition;
            _previewScene.Camera.nearClipPlane = 0.01f;
            _previewScene.Camera.clearFlags = CameraClearFlags.Skybox;

            if (!_previewScene.Camera.TryGetComponent(out _skybox))
            {
                _skybox = _previewScene.Camera.gameObject.AddComponent<Skybox>();
            }

            _skybox.enabled = _editorState.PreviewObjectSelection == PreviewObjectType.Skybox ||
                              _editorState.PreviewBgSkybox;
            _skybox.material = _materials.MatSkybox;

            CreatePreviewObjects();
            SetPreviewObject(_editorState.PreviewObjectSelection);
        }

        private void CreatePreviewObjects()
        {
            if (_settings.MeshIcosphere != null && _materials.MatSphere != null)
            {
                _previewSphere = new GameObject("U17CubeGen.PreviewSphere");
                _previewSphere.hideFlags = HideFlags.HideAndDontSave;
                _previewSphere.transform.localScale = SpherePreviewScale;
                var mf = _previewSphere.AddComponent<MeshFilter>();
                mf.mesh = _settings.MeshIcosphere;
                var mr = _previewSphere.AddComponent<MeshRenderer>();
                mr.sharedMaterial = _materials.MatSphere;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                _previewScene.AddGameObject(_previewSphere);
            }

            if (_settings.MeshSkybox != null && _materials.MatCube != null)
            {
                _previewCube = new GameObject("U17CubeGen.PreviewCube");
                _previewCube.hideFlags = HideFlags.HideAndDontSave;
                _previewCube.transform.localScale = CubePreviewScale;
                var mf = _previewCube.AddComponent<MeshFilter>();
                mf.mesh = _settings.MeshSkybox;
                var mr = _previewCube.AddComponent<MeshRenderer>();
                mr.sharedMaterial = _materials.MatCube;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                _previewScene.AddGameObject(_previewCube);
            }
        }

        public void SelectPreviewObject(PreviewObjectType type)
        {
            if (_previewSphere != null)
            {
                _previewSphere.SetActive(type == PreviewObjectType.Sphere);
            }

            if (_previewCube != null)
            {
                _previewCube.SetActive(type == PreviewObjectType.Cube);
            }
        }

        public void SetPreviewObject(PreviewObjectType type)
        {
            SelectPreviewObject(type);
            if (_skybox != null)
            {
                _skybox.enabled = type == PreviewObjectType.Skybox || _editorState.PreviewBgSkybox;
            }

            UpdateCameraFieldOfView(type);
        }

        private void UpdateCameraFieldOfView(PreviewObjectType type)
        {
            if (_previewScene.Camera != null)
            {
                var targetFov = type == PreviewObjectType.Skybox
                    ? SkyboxCameraFieldOfView
                    : ObjectCameraFieldOfView;

                if (!Mathf.Approximately(_previewScene.Camera.fieldOfView, targetFov))
                {
                    _previewScene.Camera.fieldOfView = targetFov;
                }
            }
        }

        public void SetPreviewRotation(Quaternion value)
        {
            PreviewRotation = value;
            _previewRotationMatrix = Matrix4x4.Rotate(PreviewRotation);
            UpdatePreviewRotation();
        }

        public void ApplyCubemap(Texture cubemap)
        {
            _materials.ApplyCubemap(cubemap);
            _hasCubemapBeenApplied = cubemap != null;
            if (!_hasCubemapBeenApplied && _bgContainer != null)
            {
                _bgContainer.style.backgroundImage = StyleKeyword.None;
#if UNITY_6000_2_OR_NEWER
                _renderTextureEntityId = default;
#else
                _renderTextureInstanceID = 0;
#endif
            }
        }

        public void UpdatePreviewRotation()
        {
            if (_materials.MatSkybox == null || _materials.MatSphere == null ||
                _materials.MatCube == null ||
                _previewCube == null || _previewScene.Camera == null)
            {
                return;
            }

            _previewCube.transform.rotation = PreviewRotation;
            _previewRotationMatrix = Matrix4x4.Rotate(PreviewRotation);

            if (_editorState.PreviewObjectSelection == PreviewObjectType.Skybox)
            {
                _previewScene.Camera.transform.rotation = PreviewRotation;
            }
            else
            {
                _previewScene.Camera.transform.rotation = Quaternion.identity;
            }

            _materials.MatSkybox.SetFloat(IDRotation, 0f);
            _materials.MatSphere.SetMatrix(IDPreviewRotationMatrix, _previewRotationMatrix);
            _materials.MatCube.SetMatrix(IDPreviewRotationMatrix, _previewRotationMatrix);
        }

        public void Render()
        {
            if (_previewScene == null || !_previewScene.IsValid || _editorState.HidePreview || !_hasCubemapBeenApplied)
            {
                return;
            }

            SetPreviewRotation(Quaternion.Euler(_editorState.PreviewRotation.x,
                _editorState.PreviewRotation.y, _editorState.PreviewRotation.z));

            var useSkybox = _editorState.PreviewObjectSelection == PreviewObjectType.Skybox ||
                            _editorState.PreviewBgSkybox;
            if (_skybox != null)
            {
                _skybox.enabled = useSkybox;
            }

            _previewScene.Camera.clearFlags =
                useSkybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            _previewScene.Camera.allowHDR = _editorState.RenderSceneHDR;

            _previewScene.Render();

            UpdateBackgroundTexture();
        }

        public void ResizePreview(float width, float height)
        {
            if (_previewScene == null)
            {
                return;
            }

            var superSize = _editorState.SuperSampling ? 2 : 1;
            var w = Mathf.Max(1, (int)width * superSize);
            var h = Mathf.Max(1, (int)(height - _toolbarHeight) * superSize);
            _previewScene.RenderTextureSize = new Vector2Int(w, h);
        }

        private void UpdateBackgroundTexture()
        {
            if (_previewScene.RenderTexture == null)
            {
                return;
            }

#if UNITY_6000_2_OR_NEWER
            var currentId = _previewScene.RenderTexture.GetEntityId();
            if (!_renderTextureEntityId.Equals(currentId))
            {
                _renderTextureEntityId = currentId;
                _bgContainer.style.backgroundImage =
                    new StyleBackground(Background.FromRenderTexture(_previewScene.RenderTexture));
            }
#else
            var currentId = _previewScene.RenderTexture.GetInstanceID();
            if (!_renderTextureInstanceID.Equals(currentId))
            {
                _renderTextureInstanceID = currentId;
                _bgContainer.style.backgroundImage =
                    new StyleBackground(Background.FromRenderTexture(_previewScene.RenderTexture));
            }
#endif
            _bgContainer.MarkDirtyRepaint();
        }

        private void OnPointerDown(Vector2 pos)
        {
            _previousDragPosition = pos;
        }

        private void OnPointerMove(Vector2 pos)
        {
            var delta = pos - _previousDragPosition;
            _previousDragPosition = pos;

            var rotX = _editorState.PreviewRotationXEnable ? -delta.y * DragSpeed : 0f;
            var rotY = _editorState.PreviewRotationYEnable ? delta.x * DragSpeed : 0f;

            if (Mathf.Approximately(rotX, 0f) && Mathf.Approximately(rotY, 0f))
            {
                return;
            }

            var currentEuler = _editorState.PreviewRotation;
            var newX = _editorState.PreviewRotationXEnable ? currentEuler.x + rotX : currentEuler.x;
            var newY = _editorState.PreviewRotationYEnable ? currentEuler.y + rotY : currentEuler.y;
            var newZ = currentEuler.z;

            newX = Mathf.Repeat(newX + 180f, 360f) - 180f;
            newY = Mathf.Repeat(newY + 180f, 360f) - 180f;

            var newEuler = new Vector3(newX, newY, newZ);

            PreviewRotation = Quaternion.Euler(newEuler);
            UpdatePreviewRotation();

            OnRotationChanged?.Invoke(newEuler);
        }
    }
}
