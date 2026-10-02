using System;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     CubemapGenerator の各種コントローラーの初期化・破棄・再構築など、プレビューと描画パイプラインのライフサイクルを一元管理するセッションクラス。
    /// </summary>
    public sealed class CoreManager : IDisposable
    {
        private readonly VisualElement _bgContainer;
        private readonly EditorState _editorState;
        private readonly GeneratorTabPage _generatorTabPage;
        private readonly PreviewTabPage _previewTabPage;
        private readonly VisualElement _processingIndicator;
        private readonly VisualElement _rootVisualElement;
        private readonly Settings _settings;
        private readonly float _toolbarHeight;
        private PreviewMaterials? _previewMaterials;
        private CubemapRenderTexturePool? _renderTexturePool;

        private TemporaryCamera? _temporaryCamera;

        public CoreManager(
            EditorState editorState,
            Settings settings,
            VisualElement rootVisualElement,
            VisualElement bgContainer,
            GeneratorTabPage generatorTabPage,
            PreviewTabPage previewTabPage,
            VisualElement processingIndicator,
            float toolbarHeight)
        {
            _editorState = editorState;
            _settings = settings;
            _rootVisualElement = rootVisualElement;
            _bgContainer = bgContainer;
            _generatorTabPage = generatorTabPage;
            _previewTabPage = previewTabPage;
            _processingIndicator = processingIndicator;
            _toolbarHeight = toolbarHeight;
        }

        public RenderingFlowController? RenderingFlowController { get; private set; }
        public ViewportController? ViewportController { get; private set; }
        public CameraTracker? CameraTracker { get; private set; }
        public ExportFlowController? ExportFlowController { get; private set; }
        public DragEventHandler? DragEventHandler { get; private set; }

        public void Dispose()
        {
            DisposeRuntimeObjects();
        }

        public void RebuildRuntimeObjects()
        {
            DisposeRuntimeObjects();

            _temporaryCamera = new TemporaryCamera();
            _previewMaterials = new PreviewMaterials(_settings);
            _renderTexturePool = new CubemapRenderTexturePool();
            DragEventHandler = new DragEventHandler(_bgContainer);

            DragEventHandler.OnPointerDownEvent += OnPointerDown;
            DragEventHandler.OnPointerUpEvent += OnPointerUp;

            ViewportController = new ViewportController(
                _bgContainer,
                DragEventHandler,
                _editorState,
                _settings,
                _previewMaterials,
                _toolbarHeight);

            ViewportController.OnRotationChanged += OnRotationChanged;

            RenderingFlowController = new RenderingFlowController(
                _rootVisualElement,
                _editorState,
                _settings,
                _renderTexturePool,
                _temporaryCamera.Camera,
                ViewportController,
                GetCurrentCamera);

            RenderingFlowController.OnIntermediateSizeChanged += OnIntermediateSizeChanged;
            RenderingFlowController.OnWarning += OnWarning;
            RenderingFlowController.OnProcessingStateChanged += OnProcessingStateChanged;

            CameraTracker = new CameraTracker(_editorState, _settings, GetCurrentCamera);
            CameraTracker.StartTracking(() => RenderingFlowController?.RequestRenderCubemap());

            ExportFlowController = new ExportFlowController(
                _editorState,
                _settings,
                RenderingFlowController,
                CameraTracker,
                _generatorTabPage);

            RenderingFlowController.RequestRenderCubemap();
        }

        private Camera GetCurrentCamera()
        {
            Camera excludedCam = null!;
            if (_temporaryCamera != null && _temporaryCamera.Camera != null)
            {
                excludedCam = _temporaryCamera.Camera;
            }

            return CameraResolver.ResolveCurrentCamera(_editorState, excludedCam);
        }

        public void ResetSettings()
        {
            _temporaryCamera?.ResetTransform();
        }

        public void InvalidateCameraCache()
        {
            CameraTracker?.InvalidateCameraCache();
        }

        public void RequestRenderCubemap()
        {
            RenderingFlowController?.RequestRenderCubemap();
        }

        public void RequestRenderBlurredCubemap()
        {
            RenderingFlowController?.RequestRenderBlurredCubemap();
        }

        public void RequestRedraw()
        {
            RenderingFlowController?.RequestRedraw();
        }

        public void RequestResize(float width, float height)
        {
            RenderingFlowController?.RequestResize(width, height);
        }

        public void SetPreviewObject(PreviewObjectType type)
        {
            ViewportController?.SetPreviewObject(type);
        }

        public async Awaitable OnExportAsync(CancellationToken cancellationToken = default)
        {
            if (ExportFlowController != null)
            {
                await ExportFlowController.OnExportAsync(cancellationToken);
            }
        }

        private void DisposeRuntimeObjects()
        {
            if (ExportFlowController != null)
            {
                ExportFlowController.Dispose();
                ExportFlowController = null;
            }

            CameraTracker?.Dispose();
            CameraTracker = null;

            if (RenderingFlowController != null)
            {
                RenderingFlowController.OnIntermediateSizeChanged -= OnIntermediateSizeChanged;
                RenderingFlowController.OnWarning -= OnWarning;
                RenderingFlowController.OnProcessingStateChanged -= OnProcessingStateChanged;
                RenderingFlowController.Dispose();
                RenderingFlowController = null;
            }

            if (ViewportController != null)
            {
                ViewportController.OnRotationChanged -= OnRotationChanged;
                ViewportController.Dispose();
                ViewportController = null;
            }

            if (DragEventHandler != null)
            {
                DragEventHandler.OnPointerDownEvent -= OnPointerDown;
                DragEventHandler.OnPointerUpEvent -= OnPointerUp;
                DragEventHandler.Dispose();
                DragEventHandler = null;
            }

            _temporaryCamera?.Dispose();
            _temporaryCamera = null;
            _previewMaterials?.Dispose();
            _previewMaterials = null;
            _renderTexturePool?.Dispose();
            _renderTexturePool = null;

            if (_bgContainer != null)
            {
                _bgContainer.style.backgroundImage = StyleKeyword.None;
            }

            if (_processingIndicator != null)
            {
                _processingIndicator.style.display = DisplayStyle.None;
            }
        }

        private void OnPointerDown(Vector2 pos)
        {
            Undo.RecordObject(_editorState, "Rotate Cubemap Preview");
        }

        private void OnPointerUp()
        {
            if (_editorState != null)
            {
                EditorUtility.SetDirty(_editorState);
            }

            RenderingFlowController?.RedrawImmediate();
        }

        private void OnRotationChanged(Vector3 rot)
        {
            _editorState.SetPreviewRotation(rot);
            _previewTabPage.SetRotation(_editorState.PreviewRotation);
            RenderingFlowController?.RequestRedraw();
        }

        private void OnIntermediateSizeChanged(Vector2Int size)
        {
            _previewTabPage.SetIntermediateSize(size);
        }

        private void OnWarning(string? msg)
        {
            _generatorTabPage.SetWarning(msg);
        }

        private void OnProcessingStateChanged(bool isProcessing, bool isExportReady)
        {
            _generatorTabPage.SetRedrawEnabled(!isProcessing);
            _generatorTabPage.SetExportEnabled(isExportReady);
            if (_processingIndicator != null)
            {
                _processingIndicator.style.display = isProcessing ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
