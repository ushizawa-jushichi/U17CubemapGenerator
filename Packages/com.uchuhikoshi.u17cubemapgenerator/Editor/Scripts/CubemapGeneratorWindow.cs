using System;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     U17CubemapGenerator のメインエディターウィンドウ。UI の構築と各コントローラーのライフサイクルを管理する。
    /// </summary>
    public sealed class CubemapGeneratorWindow : EditorWindow
    {
        private const string PackageName = "com.uchuhikoshi.u17cubemapgenerator";
        private const string SettingsRelativePath = "Editor/EditorResources/U17CubemapGeneratorSettings.asset";

        private AssetDragAndDropHandler? _assetDragHandler;
        private VisualElement _bgContainer = null!;
        private VisualElement _contentContainer = null!;

        private CoreManager? _coreManager;
        private EditorState _editorState = null!;
        private GeneratorTabPage _generatorTabPage = null!;
        private bool _isExportTaskRunning;
        private bool _isRebuildScheduled;
        private Type? _lastRenderPipelineType;
        private PreviewTabPage _previewTabPage = null!;

        private VisualElement _processingIndicator = null!;
        private SerializedObject _serializedState = null!;
        private Settings _settings = null!;
        private EditorStateSaver? _stateSaver;
        private Toggle _toggleHidePreview = null!;
        private Toggle _toggleHideUI = null!;
        private float _toolbarHeight;
        private ToolbarWithTabPages _toolbarWithTabPages = null!;
        private CancellationTokenSource? _windowCts;

        private void OnEnable()
        {
            _windowCts?.Cancel();
            _windowCts?.Dispose();
            _windowCts = new CancellationTokenSource();

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChangedInEditMode;
            SceneManager.sceneLoaded += OnSceneLoaded;
            RenderPipelineManager.activeRenderPipelineCreated += OnRenderPipelineCreated;

            _lastRenderPipelineType = RenderPipelineManager.currentPipeline?.GetType();
        }

        private void OnDisable()
        {
            _windowCts?.Cancel();
            _windowCts?.Dispose();
            _windowCts = null;

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChangedInEditMode;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RenderPipelineManager.activeRenderPipelineCreated -= OnRenderPipelineCreated;

            UnsubscribeTabPageEvents();

            CancelScheduledRebuild();

            rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            rootVisualElement.Unbind();

            if (_editorState != null)
            {
                if (_stateSaver != null)
                {
                    try
                    {
                        _stateSaver.SaveImmediate(_editorState);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                    _stateSaver.Dispose();
                    _stateSaver = null;
                }

                DestroyImmediate(_editorState);
                _editorState = null!;
            }

            _coreManager?.Dispose();
            _coreManager = null;

            _assetDragHandler?.Dispose();
            _assetDragHandler = null;

            _toolbarWithTabPages?.Dispose();
            _toolbarWithTabPages = null!;
            _generatorTabPage = null!;
            _previewTabPage = null!;

            _serializedState?.Dispose();
            _serializedState = null!;
        }

        public void CreateGUI()
        {
            CleanupRuntimeObjects();

            try
            {
                if (_settings == null)
                {
                    var assetPath = GetSettingsAssetPath();
                    _settings = AssetDatabase.LoadAssetAtPath<Settings>(assetPath);
                    if (_settings == null)
                    {
                        _settings = Settings.CreateNewSettingInstance(assetPath);
                    }
                }

                if (_editorState == null)
                {
                    _editorState = EditorState.Create(_settings);
                }

                _stateSaver?.Dispose();
                _stateSaver = new EditorStateSaver();

                _serializedState?.Dispose();
                _serializedState = new SerializedObject(_editorState);

                Assert.IsNotNull(_settings, $"{nameof(_settings)} is not attached.");
                Assert.IsNotNull(_editorState, $"{nameof(_editorState)} is not attached.");

                rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                if (_settings.StyleUSS != null && !rootVisualElement.styleSheets.Contains(_settings.StyleUSS))
                {
                    rootVisualElement.styleSheets.Add(_settings.StyleUSS);
                }

                _bgContainer = WindowUIBuilder.CreateBackgroundContainer();
                rootVisualElement.Add(_bgContainer);

                _generatorTabPage = new GeneratorTabPage(
                    _settings,
                    _editorState,
                    Settings.Resolutions,
                    () =>
                    {
                        _coreManager?.InvalidateCameraCache();
                        _coreManager?.RequestRenderCubemap();
                    },
                    () => _coreManager?.RequestRenderBlurredCubemap());
                _generatorTabPage.OnRedrawRequested += OnRedrawRequested;
                _generatorTabPage.OnExportRequested += OnExportRequested;

                _previewTabPage = new PreviewTabPage(
                    _settings,
                    _editorState,
                    OnRequestRedrawWindow);
                _previewTabPage.OnPreviewObjectChanged += OnPreviewObjectChanged;
                _previewTabPage.OnResetSettingsRequested += OnResetSettingsRequested;

                _contentContainer = new VisualElement { style = { flexGrow = 1 }, pickingMode = PickingMode.Ignore };

                _processingIndicator = WindowUIBuilder.CreateProcessingIndicator();

                _toolbarWithTabPages = CreateToolbar();
                _toolbarWithTabPages.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    _toolbarHeight = evt.newRect.height;
                    _bgContainer.style.top = _toolbarHeight;
                    _processingIndicator.style.top = _toolbarHeight + 8;
                });
                rootVisualElement.Add(_toolbarWithTabPages);
                rootVisualElement.Add(_contentContainer);
                rootVisualElement.Add(_processingIndicator);

                var tabIndex = Mathf.Clamp(_editorState.CurrentTabIndex, 0, _toolbarWithTabPages.Tabs.Count - 1);
                SwitchTabPage(_toolbarWithTabPages.Tabs[tabIndex].Id);

                _coreManager = new CoreManager(
                    _editorState,
                    _settings,
                    rootVisualElement,
                    _bgContainer,
                    _generatorTabPage,
                    _previewTabPage,
                    _processingIndicator,
                    _toolbarHeight);

                _coreManager.RebuildRuntimeObjects();

                rootVisualElement.Bind(_serializedState);
                rootVisualElement.TrackSerializedObjectValue(_serializedState, OnStateChanged);

                _assetDragHandler = new AssetDragAndDropHandler(
                    rootVisualElement,
                    _editorState,
                    _settings,
                    OnAssetDropped
                );
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                CleanupRuntimeObjects();
                ShowInitializationErrorUI(ex.Message);
            }
        }

        private void UnsubscribeTabPageEvents()
        {
            if (_generatorTabPage != null)
            {
                _generatorTabPage.OnRedrawRequested -= OnRedrawRequested;
                _generatorTabPage.OnExportRequested -= OnExportRequested;
            }

            if (_previewTabPage != null)
            {
                _previewTabPage.OnPreviewObjectChanged -= OnPreviewObjectChanged;
                _previewTabPage.OnResetSettingsRequested -= OnResetSettingsRequested;
            }
        }

        private void CleanupRuntimeObjects()
        {
            UnsubscribeTabPageEvents();

            rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            rootVisualElement.Unbind();
            rootVisualElement.Clear();

            _coreManager?.Dispose();
            _coreManager = null;

            _assetDragHandler?.Dispose();
            _assetDragHandler = null;

            _toolbarWithTabPages?.Dispose();
            _toolbarWithTabPages = null!;
            _generatorTabPage = null!;
            _previewTabPage = null!;

            _serializedState?.Dispose();
            _serializedState = null!;
        }

        private void ShowInitializationErrorUI(string errorMessage)
        {
            var errorBox = new HelpBox(
                $"[U17CubemapGenerator] Failed to initialize window.\nError: {errorMessage}\n\nPlease check the Console log for details.",
                HelpBoxMessageType.Error)
            {
                style =
                {
                    marginTop = 12,
                    marginBottom = 12,
                    marginLeft = 12,
                    marginRight = 12
                }
            };
            rootVisualElement.Add(errorBox);
        }

        [MenuItem("Tools/U17CubemapGenerator")]
        public static void ShowCubemapGeneratorWindow()
        {
            var wnd = GetWindow<CubemapGeneratorWindow>();
            wnd.titleContent = new GUIContent("U17CubemapGenerator");
        }

        private static string GetSettingsAssetPath()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(Settings)}");
            if (guids.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(guids[0]);
            }

            var packageInfo = PackageInfo.FindForAssembly(typeof(CubemapGeneratorWindow).Assembly);
            var rootPath = packageInfo != null ? packageInfo.assetPath : $"Packages/{PackageName}";

            return $"{rootPath}/{SettingsRelativePath}";
        }

        private ToolbarWithTabPages CreateToolbar()
        {
            var toolbar = new ToolbarWithTabPages()
                .AddTabPage(_generatorTabPage, SwitchTabPage, _contentContainer)
                .AddTabPage(_previewTabPage, SwitchTabPage, _contentContainer);

            _toggleHideUI = new Toggle("Hide UI")
            {
                bindingPath = EditorState.BindPath(nameof(_editorState.HideUI)),
                style = { maxWidth = 150 }
            };
            _toggleHideUI.AddToClassList("toolbar-checkbox");
            _toggleHideUI.RegisterValueChangedCallback(evt => OnHideUIChanged(evt.newValue));
            toolbar.Add(_toggleHideUI);

            _toggleHidePreview = new Toggle("Hide Preview")
            {
                bindingPath = EditorState.BindPath(nameof(_editorState.HidePreview)),
                style = { maxWidth = 150 }
            };
            _toggleHidePreview.AddToClassList("toolbar-checkbox");
            _toggleHidePreview.RegisterValueChangedCallback(evt => OnHidePreviewChanged(evt.newValue));
            toolbar.Add(_toggleHidePreview);

            return toolbar;
        }

        private void SwitchTabPage(string id)
        {
            var index = _toolbarWithTabPages.SwitchTabPage(id);
            if (index >= 0)
            {
                _editorState.SetCurrentTabIndex(index);
            }

            if (_toggleHideUI.value)
            {
                _toolbarWithTabPages.HideAllTabPages(true);
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.oldRect.size != evt.newRect.size)
            {
                _coreManager?.RequestResize(position.width, position.height);
                _previewTabPage.OnGeometryChanged(evt.newRect.size);
            }
        }

        private void OnHideUIChanged(bool value)
        {
            if (value)
            {
                _toolbarWithTabPages.HideAllTabPages(true);
            }
            else
            {
                var activeTabId = _toolbarWithTabPages.ActiveTabPage?.Id ?? _generatorTabPage.Id;
                SwitchTabPage(activeTabId);
            }

            _coreManager?.RequestRedraw();
        }

        private void OnHidePreviewChanged(bool value)
        {
            _bgContainer.style.display = !value ? DisplayStyle.Flex : DisplayStyle.None;
            if (!value)
            {
                _coreManager?.RequestRenderCubemap();
            }
            else
            {
                _coreManager?.RequestRedraw();
            }
        }

        private void OnStateChanged(SerializedObject serializedObj)
        {
            if (_editorState != null)
            {
                _stateSaver?.RequestSave(_editorState);
            }
        }

        private void OnAssetDropped()
        {
            if (_editorState != null)
            {
                _stateSaver?.RequestSave(_editorState);
            }

            SynchronizeAllStateAndRedraw(false);
        }

        private void OnUndoRedoPerformed()
        {
            if (_editorState == null)
            {
                return;
            }

            SynchronizeAllStateAndRedraw(true);
        }

        private void OnResetSettingsRequested()
        {
            _coreManager?.ResetSettings();

            Undo.RecordObject(_editorState, "Reset Settings");
            _editorState.ResetToDefault(_settings);
            _editorState.SetAdaptiveSRGBEnable();
            EditorUtility.SetDirty(_editorState);

            _stateSaver?.RequestSave(_editorState);

            SynchronizeAllStateAndRedraw(true);
        }

        private void SynchronizeAllStateAndRedraw(bool invalidateCameraCache)
        {
            if (_serializedState != null && _serializedState.targetObject != null)
            {
                _serializedState.Update();
            }

            _generatorTabPage?.RefreshUI();
            _previewTabPage?.RefreshUI();

            if (_editorState != null)
            {
                _coreManager?.SetPreviewObject(_editorState.PreviewObjectSelection);
            }

            if (invalidateCameraCache)
            {
                _coreManager?.InvalidateCameraCache();
            }

            _coreManager?.RequestRedraw();
            _coreManager?.RequestRenderCubemap();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    CancelScheduledRebuild();
                    _coreManager?.InvalidateCameraCache();
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                case PlayModeStateChange.EnteredEditMode:
                    ScheduleRebuildPreviewScene();
                    break;
            }
        }

        private void OnActiveSceneChangedInEditMode(Scene current, Scene next)
        {
            ScheduleRebuildPreviewScene();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // エディットモード時のシーン読み込みは OnActiveSceneChangedInEditMode が担当するため、
            // 二重発火を防止するためにプレイモード中 (isPlaying == true) のシーンロードのみ処理する。
            if (EditorApplication.isPlaying)
            {
                ScheduleRebuildPreviewScene();
            }
        }

        private void OnRenderPipelineCreated()
        {
            var currentType = RenderPipelineManager.currentPipeline?.GetType();
            if (_lastRenderPipelineType != currentType)
            {
                _lastRenderPipelineType = currentType;
                ScheduleRebuildPreviewScene();
            }
        }

        private void CancelScheduledRebuild()
        {
            if (_isRebuildScheduled)
            {
                EditorApplication.delayCall -= ExecuteRebuildPreviewScene;
                _isRebuildScheduled = false;
            }
        }

        private void ScheduleRebuildPreviewScene()
        {
            if (_isRebuildScheduled)
            {
                return;
            }

            _isRebuildScheduled = true;
            EditorApplication.delayCall += ExecuteRebuildPreviewScene;
        }

        private void ExecuteRebuildPreviewScene()
        {
            CancelScheduledRebuild();

            if (this != null && _coreManager != null)
            {
                _coreManager.RebuildRuntimeObjects();
                _previewTabPage?.RefreshUI();
            }
        }

        private void OnRedrawRequested()
        {
            _coreManager?.InvalidateCameraCache();
            _coreManager?.RequestRenderCubemap();
        }

        private void OnExportRequested()
        {
            if (_isExportTaskRunning)
            {
                return;
            }

            _isExportTaskRunning = true;
            var token = _windowCts?.Token ?? CancellationToken.None;
            _ = ExportAsync(token);

            async Awaitable ExportAsync(CancellationToken cancellationToken)
            {
                try
                {
                    if (_coreManager != null)
                    {
                        await _coreManager.OnExportAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // ユーザーによるキャンセルや中断操作時は正常終了として扱い、エラーログを出力しない
                }
                catch (ObjectDisposedException)
                {
                    // ウィンドウ破棄やコンパイルに伴う中断時はエラーログを出力しない
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CubemapGenerator] Export failed: {ex.Message}");
                }
                finally
                {
                    _isExportTaskRunning = false;
                }
            }
        }

        private void OnPreviewObjectChanged(PreviewObjectType type)
        {
            _coreManager?.SetPreviewObject(type);
        }

        private void OnRequestRedrawWindow()
        {
            _coreManager?.RequestRedraw();
        }
    }
}
