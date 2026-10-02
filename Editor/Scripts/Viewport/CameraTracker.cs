using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     シーンカメラのパラメータや位置・回転の変更を監視し、プレビューの再描画を要求するトラッカー。
    /// </summary>
    public sealed class CameraTracker : IDisposable
    {
        private readonly EditorState _editorState;
        private readonly Func<Camera> _getCurrentCamera;
        private readonly Settings _settings;

        private UniversalAdditionalCameraData _additionalCameraData = null!;
        private Color _backgroundColorCache;
        private CameraClearFlags _clearFlagsCache;
        private int _cullingMaskCache;
        private Camera _currentCameraCache = null!;
        private float _farClipPlaneCache;
        private bool _isTracking;

        private double _lastPollTime;
        private float _nearClipPlaneCache;
        private bool _needsCameraRefresh = true;
        private Action? _onRedraw;
        private Vector3 _positionCache;
        private bool _renderPostProcessingCache;
        private bool _renderShadowsCache;
        private Quaternion _rotationCache;
        private bool _useOcclusionCullingCache;
        private LayerMask _volumeLayerMaskCache;
        private Transform _volumeTriggerCache = null!;

        public CameraTracker(EditorState editorState, Settings settings,
            Func<Camera> getCurrentCamera)
        {
            _editorState = editorState;
            _settings = settings;
            _getCurrentCamera = getCurrentCamera;

            Assert.IsNotNull(_editorState, $"{nameof(_editorState)} is null.");
            Assert.IsNotNull(_settings, $"{nameof(_settings)} is null.");
            Assert.IsNotNull(_getCurrentCamera, $"{nameof(_getCurrentCamera)} is null.");
        }

        public bool IsPaused { get; set; }

        public void Dispose()
        {
            StopTracking();
        }

        private bool HasPositionChanged(in Vector3 a, in Vector3 b)
        {
            var threshold = _settings.CameraTrackingPositionThreshold;
            return (a - b).sqrMagnitude > threshold * threshold;
        }

        private bool HasRotationChanged(in Quaternion a, in Quaternion b)
        {
            return Quaternion.Angle(a, b) > _settings.CameraTrackingRotationAngleThreshold;
        }

        public void StartTracking(Action onRedraw)
        {
            if (_isTracking)
            {
                return;
            }

            _onRedraw = onRedraw;
            _isTracking = true;
            _needsCameraRefresh = true;

            ObjectChangeEvents.changesPublished += OnChangesPublished;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.update += OnEditorUpdate;

            Evaluate();
        }

        public void StopTracking()
        {
            if (!_isTracking)
            {
                return;
            }

            ObjectChangeEvents.changesPublished -= OnChangesPublished;
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.update -= OnEditorUpdate;
            _onRedraw = null;
            _isTracking = false;
        }

        public void InvalidateCameraCache()
        {
            _needsCameraRefresh = true;
            Evaluate();
        }

        private void OnHierarchyChanged()
        {
            _needsCameraRefresh = true;
            Evaluate();
        }

        private void OnEditorUpdate()
        {
            if (IsPaused || _editorState == null || _editorState.InputMode != InputModeType.CurrentScene ||
                _editorState.HidePreview)
            {
                return;
            }

            var currentTime = EditorApplication.timeSinceStartup;
            var interval = EditorApplication.isPlaying
                ? _settings.CameraTrackingPlayModePollInterval
                : _settings.CameraTrackingIdlePollInterval;

            if (currentTime - _lastPollTime < interval)
            {
                return;
            }

            _lastPollTime = currentTime;
            Evaluate();
        }

        private void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            if (IsPaused || _editorState == null || _editorState.HidePreview || _currentCameraCache == null)
            {
                return;
            }

            if (_editorState.RenderSceneSyncSceneViewCamera)
            {
                return;
            }

#if UNITY_6000_4_OR_NEWER
            var cameraEntityId = _currentCameraCache.GetEntityId();
            var transformEntityId = _currentCameraCache.transform.GetEntityId();
            var additionalDataEntityId =
                _additionalCameraData != null ? _additionalCameraData.GetEntityId() : EntityId.None;
#else
            var cameraInstanceId = _currentCameraCache.GetInstanceID();
            var transformInstanceId = _currentCameraCache.transform.GetInstanceID();
            var additionalDataInstanceId = _additionalCameraData != null ? _additionalCameraData.GetInstanceID() : 0;
#endif

            for (var i = 0; i < stream.length; i++)
            {
                var type = stream.GetEventType(i);
                if (type == ObjectChangeKind.ChangeGameObjectOrComponentProperties)
                {
                    stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var data);
#if UNITY_6000_4_OR_NEWER
                    if (data.entityId == cameraEntityId ||
                        data.entityId == transformEntityId ||
                        (additionalDataEntityId != EntityId.None && data.entityId == additionalDataEntityId))
#else
                    if (data.instanceId == cameraInstanceId ||
                        data.instanceId == transformInstanceId ||
                        (additionalDataInstanceId != 0 && data.instanceId == additionalDataInstanceId))
#endif
                    {
                        Evaluate();
                        break;
                    }
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!IsPaused && _editorState != null && _editorState.RenderSceneSyncSceneViewCamera &&
                !_editorState.HidePreview)
            {
                Evaluate();
            }
        }

        private void Evaluate()
        {
            if (IsPaused || _editorState == null || _editorState.InputMode != InputModeType.CurrentScene ||
                _editorState.HidePreview)
            {
                return;
            }

            Camera currentCamera;
            if (_needsCameraRefresh || _currentCameraCache == null ||
                _editorState.RenderSceneSyncSceneViewCamera)
            {
                currentCamera = _getCurrentCamera.Invoke();
                _needsCameraRefresh = false;
            }
            else
            {
                currentCamera = _currentCameraCache;
            }

            var targetCamera = currentCamera != null ? currentCamera : null;
            var modified = false;

            if (_currentCameraCache != targetCamera
                || (_currentCameraCache == null && !ReferenceEquals(_currentCameraCache, null))
                || (_additionalCameraData == null && !ReferenceEquals(_additionalCameraData, null)))
            {
                _currentCameraCache = targetCamera!;
                modified = true;

                if (_currentCameraCache != null)
                {
                    _currentCameraCache.TryGetComponent(out _additionalCameraData);
                }
                else
                {
                    _additionalCameraData = null!;
                }
            }

            if (_currentCameraCache != null)
            {
                var currentPos = _currentCameraCache.transform.position;
                var currentRot = _currentCameraCache.transform.rotation;

                if (modified
                    || HasPositionChanged(_positionCache, currentPos)
                    || HasRotationChanged(_rotationCache, currentRot)
                    || !Mathf.Approximately(_nearClipPlaneCache, _currentCameraCache.nearClipPlane)
                    || !Mathf.Approximately(_farClipPlaneCache, _currentCameraCache.farClipPlane)
                    || _clearFlagsCache != _currentCameraCache.clearFlags
                    || _backgroundColorCache != _currentCameraCache.backgroundColor
                    || _cullingMaskCache != _currentCameraCache.cullingMask
                    || _useOcclusionCullingCache != _currentCameraCache.useOcclusionCulling
                   )
                {
                    _positionCache = currentPos;
                    _rotationCache = currentRot;
                    _nearClipPlaneCache = _currentCameraCache.nearClipPlane;
                    _farClipPlaneCache = _currentCameraCache.farClipPlane;
                    _clearFlagsCache = _currentCameraCache.clearFlags;
                    _backgroundColorCache = _currentCameraCache.backgroundColor;
                    _cullingMaskCache = _currentCameraCache.cullingMask;
                    _useOcclusionCullingCache = _currentCameraCache.useOcclusionCulling;
                    modified = true;
                }

                if (_additionalCameraData != null)
                {
                    if (modified
                        || _renderPostProcessingCache != _additionalCameraData.renderPostProcessing
                        || _renderShadowsCache != _additionalCameraData.renderShadows
                        || _volumeLayerMaskCache != _additionalCameraData.volumeLayerMask
                        || _volumeTriggerCache != _additionalCameraData.volumeTrigger)
                    {
                        _renderPostProcessingCache = _additionalCameraData.renderPostProcessing;
                        _renderShadowsCache = _additionalCameraData.renderShadows;
                        _volumeLayerMaskCache = _additionalCameraData.volumeLayerMask;
                        _volumeTriggerCache = _additionalCameraData.volumeTrigger;
                        modified = true;
                    }
                }
            }
            else
            {
                if (modified)
                {
                    _positionCache = Vector3.zero;
                    _rotationCache = Quaternion.identity;
                    _nearClipPlaneCache = 0f;
                    _farClipPlaneCache = 0f;
                    _clearFlagsCache = default;
                    _backgroundColorCache = default;
                    _cullingMaskCache = 0;
                    _useOcclusionCullingCache = false;
                    _renderPostProcessingCache = false;
                    _renderShadowsCache = false;
                    _volumeLayerMaskCache = default;
                    _volumeTriggerCache = null!;
                }
            }

            if (modified)
            {
                _onRedraw?.Invoke();
            }
        }
    }
}
