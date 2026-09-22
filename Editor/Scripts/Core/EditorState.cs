using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     エディターウィンドウの設定や入力パラメータをプロジェクト別に EditorPrefs に保存・永続化する状態クラス。
    /// </summary>
    public class EditorState : ScriptableObject
    {
        // プロジェクトパス (Application.dataPath) の MD5 ハッシュを用いて決定論的なキーを生成
        private static string? _prefKey;

        [SerializeField] private bool _initialized;

        [SerializeField] private int _currentTabIndex;

        [SerializeField] private bool _hideUI;

        [SerializeField] private bool _hidePreview;

        [SerializeField] private PreviewObjectType _previewObjectSelection;

        [SerializeField] private bool _superSampling;

        [SerializeField] private bool _previewBgSkybox = true;

        [SerializeField] private Vector3 _previewRotation = Vector3.zero;

        [SerializeField] private bool _previewRotationXEnable;

        [SerializeField] private bool _previewRotationYEnable;

        [SerializeField] private bool _previewRotationZEnable;

        [SerializeField] private InputModeType _inputMode;

        [SerializeField] private bool _renderSceneSyncSceneViewCamera;

        [SerializeField] private bool _renderSceneRotatable = true;

        [SerializeField] private bool _renderSceneHDR;

        [SerializeField] private bool _renderSceneSRGB = true;

        [SerializeField] private Texture2D _sixSidedXPlus = null!;

        [SerializeField] private Texture2D _sixSidedXMinus = null!;

        [SerializeField] private Texture2D _sixSidedYPlus = null!;

        [SerializeField] private Texture2D _sixSidedYMinus = null!;

        [SerializeField] private Texture2D _sixSidedZPlus = null!;

        [SerializeField] private Texture2D _sixSidedZMinus = null!;

        [SerializeField] private Cubemap _cubemap = null!;

        [SerializeField] private bool[] _faceFlipH = new bool[6];

        [SerializeField] private bool[] _faceFlipV = new bool[6];

        [SerializeField] private bool _blurEnable = true;

        [SerializeField] private CubemapBlurAlgorithm _blurAlgorithm;

        [SerializeField] private float _blurRoughness_GGX_SpecularIBL = 0.15f;

        [SerializeField] private int _blurNumSamples_GGX_SpecularIBL = 128;

        [SerializeField] private float _blurRoughness_Gaussian_Bokeh = 0.15f;

        [SerializeField] private int _blurNumSamples_Gaussian_Bokeh = 128;

        [SerializeField] private int _outputResolution = 1024;

        [SerializeField] private string _exportPath = string.Empty;

        [SerializeField] private OutputLayoutType _outputLayout = OutputLayoutType.CrossHorizontal;

        [SerializeField] private float _equirectangularY;

        [SerializeField] private bool _equirectangularImportAsCubemap;

        [SerializeField] private bool _crossOrStraightImportAsCubemap = true;

        [SerializeField] private bool _matcapFillOutside = true;

        [SerializeField] private Camera _renderSceneCamera = null!;
        private static string PrefKey => _prefKey ??= ComputePrefKey();

        public bool Initialized => _initialized;
        public int CurrentTabIndex => _currentTabIndex;
        public bool HideUI => _hideUI;
        public bool HidePreview => _hidePreview;

        public PreviewObjectType PreviewObjectSelection => _previewObjectSelection;
        public bool SuperSampling => _superSampling;
        public bool PreviewBgSkybox => _previewBgSkybox;
        public Vector3 PreviewRotation => _previewRotation;
        public bool PreviewRotationXEnable => _previewRotationXEnable;
        public bool PreviewRotationYEnable => _previewRotationYEnable;
        public bool PreviewRotationZEnable => _previewRotationZEnable;

        public InputModeType InputMode => _inputMode;
        public bool RenderSceneSyncSceneViewCamera => _renderSceneSyncSceneViewCamera;
        public bool RenderSceneRotatable => _renderSceneRotatable;
        public bool RenderSceneHDR => _renderSceneHDR;
        public bool RenderSceneSRGB => _renderSceneSRGB;
        public Texture2D SixSidedXPlus => _sixSidedXPlus;
        public Texture2D SixSidedXMinus => _sixSidedXMinus;
        public Texture2D SixSidedYPlus => _sixSidedYPlus;
        public Texture2D SixSidedYMinus => _sixSidedYMinus;
        public Texture2D SixSidedZPlus => _sixSidedZPlus;
        public Texture2D SixSidedZMinus => _sixSidedZMinus;
        public Cubemap Cubemap => _cubemap;

        public bool BlurEnable => _blurEnable;
        public CubemapBlurAlgorithm BlurAlgorithm => _blurAlgorithm;
        public float BlurRoughness_GGX_SpecularIBL => _blurRoughness_GGX_SpecularIBL;
        public int BlurNumSamples_GGX_SpecularIBL => _blurNumSamples_GGX_SpecularIBL;
        public float BlurRoughness_Gaussian_Bokeh => _blurRoughness_Gaussian_Bokeh;
        public int BlurNumSamples_Gaussian_Bokeh => _blurNumSamples_Gaussian_Bokeh;

        public int OutputResolution => _outputResolution;
        public string ExportPath => _exportPath;
        public OutputLayoutType OutputLayout => _outputLayout;
        public float EquirectangularY => _equirectangularY;
        public bool EquirectangularImportAsCubemap => _equirectangularImportAsCubemap;
        public bool CrossOrStraightImportAsCubemap => _crossOrStraightImportAsCubemap;
        public bool MatcapFillOutside => _matcapFillOutside;
        public Camera RenderSceneCamera => _renderSceneCamera;

        private void OnValidate()
        {
            _outputResolution = Mathf.Max(16, _outputResolution);
            _blurRoughness_GGX_SpecularIBL = Mathf.Clamp01(_blurRoughness_GGX_SpecularIBL);
            _blurRoughness_Gaussian_Bokeh = Mathf.Clamp01(_blurRoughness_Gaussian_Bokeh);
            _blurNumSamples_GGX_SpecularIBL = Mathf.Clamp(_blurNumSamples_GGX_SpecularIBL, 16, 4096);
            _blurNumSamples_Gaussian_Bokeh = Mathf.Clamp(_blurNumSamples_Gaussian_Bokeh, 16, 4096);
            _equirectangularY = Mathf.Repeat(_equirectangularY, 360f);
        }

        /// <summary>
        ///     プロパティ名 (例: nameof(HideUI)) を UI Toolkit バインディングパス ("_hideUI") に変換します。
        /// </summary>
        public static string BindPath(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return string.Empty;
            }

            return "_" + char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
        }

        /// <summary>
        ///     プレビュー回転角度を更新する（有効な軸のみ適用）。
        /// </summary>
        public void SetPreviewRotation(Vector3 rawRotation)
        {
            _previewRotation = new Vector3(
                _previewRotationXEnable ? rawRotation.x : _previewRotation.x,
                _previewRotationYEnable ? rawRotation.y : _previewRotation.y,
                _previewRotationZEnable ? rawRotation.z : _previewRotation.z);
        }

        /// <summary>
        ///     プレビュー回転角度をリセットする。
        /// </summary>
        public void ResetPreviewRotation(Vector3 defaultRotation)
        {
            _previewRotation = defaultRotation;
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        ///     指定された面の水平（Horizontal）フリップ状態を取得します。
        /// </summary>
        public bool GetFaceFlipH(int faceIndex)
        {
            return faceIndex >= 0 && faceIndex < _faceFlipH.Length && _faceFlipH[faceIndex];
        }

        /// <summary>
        ///     指定された面の垂直（Vertical）フリップ状態を取得します。
        /// </summary>
        public bool GetFaceFlipV(int faceIndex)
        {
            return faceIndex >= 0 && faceIndex < _faceFlipV.Length && _faceFlipV[faceIndex];
        }

        /// <summary>
        ///     指定された面の水平（Horizontal）フリップ状態を設定します。
        /// </summary>
        public void SetFaceFlipH(int faceIndex, bool value)
        {
            if (faceIndex >= 0 && faceIndex < _faceFlipH.Length && _faceFlipH[faceIndex] != value)
            {
                Undo.RecordObject(this, "Toggle Face Flip Horizontal");
                _faceFlipH[faceIndex] = value;
                EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        ///     指定された面の垂直（Vertical）フリップ状態を設定します。
        /// </summary>
        public void SetFaceFlipV(int faceIndex, bool value)
        {
            if (faceIndex >= 0 && faceIndex < _faceFlipV.Length && _faceFlipV[faceIndex] != value)
            {
                Undo.RecordObject(this, "Toggle Face Flip Vertical");
                _faceFlipV[faceIndex] = value;
                EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        ///     指定された面のフリップ状態を初期状態（反転なし）にリセットします。
        /// </summary>
        public void ResetFaceFlip(int faceIndex)
        {
            if (faceIndex >= 0 && faceIndex < 6)
            {
                var changed = _faceFlipH[faceIndex] || _faceFlipV[faceIndex];
                if (changed)
                {
                    Undo.RecordObject(this, "Reset Face Flip");
                    _faceFlipH[faceIndex] = false;
                    _faceFlipV[faceIndex] = false;
                    EditorUtility.SetDirty(this);
                }
            }
        }

        /// <summary>
        ///     すべての面のフリップ状態を初期状態（反転なし）にリセットします。
        /// </summary>
        public void ResetAllFlips()
        {
            var changed = false;
            for (var i = 0; i < 6; i++)
            {
                if (_faceFlipH[i] || _faceFlipV[i])
                {
                    changed = true;
                    break;
                }
            }

            if (changed)
            {
                Undo.RecordObject(this, "Reset All Face Flips");
                for (var i = 0; i < 6; i++)
                {
                    _faceFlipH[i] = false;
                    _faceFlipV[i] = false;
                }

                EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        ///     ドラッグ＆ドロップで Cubemap が投入された際の状態更新。
        /// </summary>
        public void SetDroppedCubemap(Cubemap cubemap)
        {
            _inputMode = InputModeType.Cubemap;
            _cubemap = cubemap;
            ResetAllFlips();
            SetAdaptiveSRGBEnable();
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        ///     ドラッグ＆ドロップで SixSided テクスチャが投入された際の状態更新。
        /// </summary>
        /// <param name="texture">投入されたテクスチャ</param>
        /// <param name="settings">サフィックス設定を持つ Settings オブジェクト</param>
        /// <param name="isSingleDrop">単一ファイルのみのドロップか（未判定時に X+ にフォールバックするか）</param>
        /// <returns>スロットへの割り当てに成功した場合は true</returns>
        public bool SetDroppedSixSidedTexture(Texture2D texture, Settings settings,
            bool isSingleDrop = false)
        {
            if (texture == null || settings == null)
            {
                return false;
            }

            _inputMode = InputModeType.SixSided;
            var faceIndex = settings.ResolveSixSidedFaceIndex(texture.name);

            switch (faceIndex)
            {
                case 0:
                    _sixSidedXPlus = texture;
                    ResetFaceFlip(0);
                    break;
                case 1:
                    _sixSidedXMinus = texture;
                    ResetFaceFlip(1);
                    break;
                case 2:
                    _sixSidedYPlus = texture;
                    ResetFaceFlip(2);
                    break;
                case 3:
                    _sixSidedYMinus = texture;
                    ResetFaceFlip(3);
                    break;
                case 4:
                    _sixSidedZPlus = texture;
                    ResetFaceFlip(4);
                    break;
                case 5:
                    _sixSidedZMinus = texture;
                    ResetFaceFlip(5);
                    break;
                default:
                    // サフィックス判定できなかった場合:
                    // 1枚のみのドロップ時のみ X+ へ割り当てる。
                    // 複数ドロップ時は他の面を上書き破壊しないようスキップする。
                    if (isSingleDrop)
                    {
                        _sixSidedXPlus = texture;
                        ResetFaceFlip(0);
                        break;
                    }

                    return false;
            }

            SetAdaptiveSRGBEnable();
            EditorUtility.SetDirty(this);
            return true;
        }

        /// <summary>
        ///     タブページのインデックスを更新する。
        /// </summary>
        public void SetCurrentTabIndex(int index)
        {
            _currentTabIndex = index;
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        ///     エクスポート先のファイルパスを更新する。
        /// </summary>
        public void SetExportPath(string path)
        {
            _exportPath = path;
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        ///     出力解像度を更新する。
        /// </summary>
        public void SetOutputResolution(int resolution)
        {
            _outputResolution = resolution;
            EditorUtility.SetDirty(this);
        }


        private static string ComputePrefKey()
        {
            var path = Application.dataPath;
            var hash = 14695981039346656037UL;
            for (var i = 0; i < path.Length; i++)
            {
                hash ^= path[i];
                hash *= 1099511628211UL;
            }

            return $"U17CubeGen_{hash:x16}_State";
        }

        public static EditorState Create(Settings settings)
        {
            var state = CreateInstance<EditorState>();
            state.hideFlags = HideFlags.DontSave;
            state.LoadState(settings);
            return state;
        }

        public void ResetToDefault(Settings settings)
        {
            Assert.IsNotNull(settings, $"{nameof(settings)} is not attached.");

            _initialized = true;
            _currentTabIndex = 0;
            _hideUI = settings.DefaultHideUI;
            _hidePreview = settings.DefaultHidePreview;

            _previewObjectSelection = settings.DefaultPreviewObject;
            _superSampling = settings.DefaultSuperSampling;
            _previewBgSkybox = settings.DefaultPreviewBgSkybox;
            _previewRotation = settings.DefaultPreviewRotation;
            _previewRotationXEnable = settings.DefaultPreviewRotationXEnable;
            _previewRotationYEnable = settings.DefaultPreviewRotationYEnable;
            _previewRotationZEnable = settings.DefaultPreviewRotationZEnable;

            _inputMode = settings.DefaultInputMode;
            _renderSceneCamera = null!;
            _renderSceneSyncSceneViewCamera = settings.DefaultRenderSceneSyncSceneViewCamera;
            _renderSceneHDR = settings.DefaultRenderSceneHDR;
            _renderSceneSRGB = settings.DefaultRenderSceneSRGB;
            _renderSceneRotatable = settings.DefaultRenderSceneRotatable;
            _sixSidedXPlus = settings.DefaultSixSidedXPlus;
            _sixSidedXMinus = settings.DefaultSixSidedXMinus;
            _sixSidedYPlus = settings.DefaultSixSidedYPlus;
            _sixSidedYMinus = settings.DefaultSixSidedYMinus;
            _sixSidedZPlus = settings.DefaultSixSidedZPlus;
            _sixSidedZMinus = settings.DefaultSixSidedZMinus;
            _cubemap = settings.DefaultCubemap;

            _blurEnable = settings.DefaultBlurEnable;
            _blurAlgorithm = settings.DefaultBlurAlgorithm;
            _blurRoughness_GGX_SpecularIBL = settings.DefaultBlurRoughness_GGX_SpecularIBL;
            _blurNumSamples_GGX_SpecularIBL = settings.DefaultBlurNumSamples_GGX_SpecularIBL;
            _blurRoughness_Gaussian_Bokeh = settings.DefaultBlurRoughness_Gaussian_Bokeh;
            _blurNumSamples_Gaussian_Bokeh = settings.DefaultBlurNumSamples_Gaussian_Bokeh;

            _outputResolution = settings.DefaultOutputResolution;
            _exportPath = settings.DefaultExportPath;
            _outputLayout = settings.DefaultOutputLayout;
            _equirectangularY = settings.DefaultEquirectangularY;
            _equirectangularImportAsCubemap = settings.DefaultEquirectangularImportAsCubemap;
            _crossOrStraightImportAsCubemap = settings.DefaultCrossOrStraightImportAsCubemap;
            _matcapFillOutside = settings.DefaultMatcapFillOutside;
            ResetAllFlips();
        }

        public void SaveState()
        {
            var data = new StateData
            {
                Initialized = _initialized,
                CurrentTabIndex = _currentTabIndex,
                HideUI = _hideUI,
                HidePreview = _hidePreview,

                PreviewObjectSelection = _previewObjectSelection,
                SuperSampling = _superSampling,
                PreviewBgSkybox = _previewBgSkybox,
                PreviewRotation = _previewRotation,
                PreviewRotationXEnable = _previewRotationXEnable,
                PreviewRotationYEnable = _previewRotationYEnable,
                PreviewRotationZEnable = _previewRotationZEnable,

                InputMode = _inputMode,
                RenderSceneSyncSceneViewCamera = _renderSceneSyncSceneViewCamera,
                RenderSceneRotatable = _renderSceneRotatable,
                RenderSceneHDR = _renderSceneHDR,
                RenderSceneSRGB = _renderSceneSRGB,
                SixSidedXPlusGuid = GetAssetGuid(_sixSidedXPlus),
                SixSidedXMinusGuid = GetAssetGuid(_sixSidedXMinus),
                SixSidedYPlusGuid = GetAssetGuid(_sixSidedYPlus),
                SixSidedYMinusGuid = GetAssetGuid(_sixSidedYMinus),
                SixSidedZPlusGuid = GetAssetGuid(_sixSidedZPlus),
                SixSidedZMinusGuid = GetAssetGuid(_sixSidedZMinus),
                CubemapGuid = GetAssetGuid(_cubemap),

                FaceFlipH = (bool[])_faceFlipH.Clone(),
                FaceFlipV = (bool[])_faceFlipV.Clone(),

                BlurEnable = _blurEnable,
                BlurAlgorithm = _blurAlgorithm,
                BlurRoughness_GGX_SpecularIBL = _blurRoughness_GGX_SpecularIBL,
                BlurNumSamples_GGX_SpecularIBL = _blurNumSamples_GGX_SpecularIBL,
                BlurRoughness_Gaussian_Bokeh = _blurRoughness_Gaussian_Bokeh,
                BlurNumSamples_Gaussian_Bokeh = _blurNumSamples_Gaussian_Bokeh,

                OutputResolution = _outputResolution,
                ExportPath = _exportPath,
                OutputLayout = _outputLayout,
                EquirectangularY = _equirectangularY,
                EquirectangularImportAsCubemap = _equirectangularImportAsCubemap,
                CrossOrStraightImportAsCubemap = _crossOrStraightImportAsCubemap,
                MatcapFillOutside = _matcapFillOutside
            };

            // RenderSceneCamera の保存
            if (_renderSceneCamera != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(_renderSceneCamera);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    // Prefab カメラ: AssetDatabase GUID で保存
                    data.RenderSceneCameraAssetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                }
                else
                {
                    // シーンカメラ: GlobalObjectId 文字列で保存
                    // シーンが未保存 (GUID が空) の場合は GlobalObjectId が "GlobalObjectId/0-0-0" になるため保存しない
                    var goid = GlobalObjectId.GetGlobalObjectIdSlow(_renderSceneCamera);
                    if (goid.assetGUID.ToString() != "00000000000000000000000000000000")
                    {
                        data.RenderSceneCameraSceneId = goid.ToString();
                    }
                }
            }

            var json = EditorJsonUtility.ToJson(data);
            EditorPrefs.SetString(PrefKey, json);
        }

        public void LoadState(Settings settings)
        {
            if (!EditorPrefs.HasKey(PrefKey))
            {
                ResetToDefault(settings);
                SetAdaptiveSRGBEnable();
                return;
            }

            try
            {
                var json = EditorPrefs.GetString(PrefKey);
                var data = new StateData();
                EditorJsonUtility.FromJsonOverwrite(json, data);

                _initialized = data.Initialized;
                _currentTabIndex = data.CurrentTabIndex;
                _hideUI = data.HideUI;
                _hidePreview = data.HidePreview;

                _previewObjectSelection = data.PreviewObjectSelection;
                _superSampling = data.SuperSampling;
                _previewBgSkybox = data.PreviewBgSkybox;
                _previewRotation = data.PreviewRotation;
                _previewRotationXEnable = data.PreviewRotationXEnable;
                _previewRotationYEnable = data.PreviewRotationYEnable;
                _previewRotationZEnable = data.PreviewRotationZEnable;

                _inputMode = data.InputMode;

                // RenderSceneCamera の復元: Prefab カメラ → GUID, シーンカメラ → GlobalObjectId
                _renderSceneCamera = null!;
                if (!string.IsNullOrEmpty(data.RenderSceneCameraAssetGuid))
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(data.RenderSceneCameraAssetGuid);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                        _renderSceneCamera = go != null ? go.GetComponentInChildren<Camera>(true)! : null!;
                    }
                }
                else if (!string.IsNullOrEmpty(data.RenderSceneCameraSceneId))
                {
                    // シーンカメラ: 対象シーンが開いていなければ null になる
                    if (GlobalObjectId.TryParse(data.RenderSceneCameraSceneId, out var goid))
                    {
                        var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(goid);
                        _renderSceneCamera = (obj as Camera)!;
                    }
                }

                _renderSceneSyncSceneViewCamera = data.RenderSceneSyncSceneViewCamera;
                _renderSceneRotatable = data.RenderSceneRotatable;
                _renderSceneHDR = data.RenderSceneHDR;
                _renderSceneSRGB = data.RenderSceneSRGB;

                // GUID からアセットを復元。未設定または Missing なら null (None)
                _sixSidedXPlus = LoadAssetFromGuid<Texture2D>(data.SixSidedXPlusGuid);
                _sixSidedXMinus = LoadAssetFromGuid<Texture2D>(data.SixSidedXMinusGuid);
                _sixSidedYPlus = LoadAssetFromGuid<Texture2D>(data.SixSidedYPlusGuid);
                _sixSidedYMinus = LoadAssetFromGuid<Texture2D>(data.SixSidedYMinusGuid);
                _sixSidedZPlus = LoadAssetFromGuid<Texture2D>(data.SixSidedZPlusGuid);
                _sixSidedZMinus = LoadAssetFromGuid<Texture2D>(data.SixSidedZMinusGuid);
                _cubemap = LoadAssetFromGuid<Cubemap>(data.CubemapGuid);

                if (data.FaceFlipH != null && data.FaceFlipH.Length == 6)
                {
                    Array.Copy(data.FaceFlipH, _faceFlipH, 6);
                }
                else
                {
                    Array.Clear(_faceFlipH, 0, 6);
                }

                if (data.FaceFlipV != null && data.FaceFlipV.Length == 6)
                {
                    Array.Copy(data.FaceFlipV, _faceFlipV, 6);
                }
                else
                {
                    Array.Clear(_faceFlipV, 0, 6);
                }

                _blurEnable = data.BlurEnable;
                _blurAlgorithm = data.BlurAlgorithm;
                _blurRoughness_GGX_SpecularIBL = data.BlurRoughness_GGX_SpecularIBL;
                _blurNumSamples_GGX_SpecularIBL = data.BlurNumSamples_GGX_SpecularIBL;
                _blurRoughness_Gaussian_Bokeh = data.BlurRoughness_Gaussian_Bokeh;
                _blurNumSamples_Gaussian_Bokeh = data.BlurNumSamples_Gaussian_Bokeh;

                _outputResolution =
                    data.OutputResolution > 0 ? data.OutputResolution : settings.DefaultOutputResolution;
                _exportPath = !string.IsNullOrEmpty(data.ExportPath) ? data.ExportPath : settings.DefaultExportPath;
                _outputLayout = data.OutputLayout;
                _equirectangularY = data.EquirectangularY;
                _equirectangularImportAsCubemap = data.EquirectangularImportAsCubemap;
                _crossOrStraightImportAsCubemap = data.CrossOrStraightImportAsCubemap;
                _matcapFillOutside = data.MatcapFillOutside;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CubemapGenerator] Failed to load saved state. Resetting to defaults. {ex.Message}");
                ResetToDefault(settings);
                SetAdaptiveSRGBEnable();
            }
        }

        private static string GetAssetGuid(Object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }

            var path = AssetDatabase.GetAssetPath(obj);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }

        private static T LoadAssetFromGuid<T>(string guid) where T : Object
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null!;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                return null!;
            }

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        public void SetAdaptiveSRGBEnable()
        {
            if (_inputMode == InputModeType.CurrentScene)
            {
                _renderSceneSRGB = true;
            }
            else if (_inputMode == InputModeType.Cubemap)
            {
                if (_cubemap != null)
                {
                    _renderSceneSRGB = _cubemap.isDataSRGB;
                }
            }
            else if (_inputMode == InputModeType.SixSided)
            {
                var firstTex = _sixSidedXPlus ? _sixSidedXPlus :
                    _sixSidedXMinus ? _sixSidedXMinus :
                    _sixSidedYPlus ? _sixSidedYPlus :
                    _sixSidedYMinus ? _sixSidedYMinus :
                    _sixSidedZPlus ? _sixSidedZPlus : _sixSidedZMinus;

                if (firstTex != null)
                {
                    _renderSceneSRGB = firstTex.isDataSRGB;
                }
            }
        }

        /// <summary>
        ///     現在の入力モード（CurrentScene, Cubemap, SixSided）に基づいて、入力データが HDR 形式かどうかを判定します。
        /// </summary>
        public bool IsCurrentInputHDR()
        {
            if (_inputMode == InputModeType.CurrentScene)
            {
                return _renderSceneHDR;
            }

            if (_inputMode == InputModeType.Cubemap)
            {
                return _cubemap != null && GraphicsFormatUtility.IsHDRFormat(_cubemap.graphicsFormat);
            }

            if (_inputMode == InputModeType.SixSided)
            {
                var firstTex = _sixSidedXPlus ? _sixSidedXPlus :
                    _sixSidedXMinus ? _sixSidedXMinus :
                    _sixSidedYPlus ? _sixSidedYPlus :
                    _sixSidedYMinus ? _sixSidedYMinus :
                    _sixSidedZPlus ? _sixSidedZPlus : _sixSidedZMinus;

                return firstTex != null && GraphicsFormatUtility.IsHDRFormat(firstTex.graphicsFormat);
            }

            return false;
        }

        #region Serialization DTO

        /// <summary>
        ///     EditorPrefs への JSON 保存用データ構造。
        ///     UnityEngine.Object 参照は instanceID ではなくアセット GUID 文字列で保持する。
        /// </summary>
        [Serializable]
        private sealed class StateData
        {
            public bool Initialized;
            public int CurrentTabIndex;
            public bool HideUI;
            public bool HidePreview;

            public PreviewObjectType PreviewObjectSelection;
            public bool SuperSampling;
            public bool PreviewBgSkybox;
            public Vector3 PreviewRotation;
            public bool PreviewRotationXEnable;
            public bool PreviewRotationYEnable;
            public bool PreviewRotationZEnable;

            public InputModeType InputMode;
            public bool RenderSceneSyncSceneViewCamera;
            public bool RenderSceneRotatable;
            public bool RenderSceneHDR;
            public bool RenderSceneSRGB;

            // RenderSceneCamera の永続化:
            //   Prefab カメラ → RenderSceneCameraAssetGuid に AssetDatabase GUID を保存
            //   シーンカメラ  → RenderSceneCameraSceneId に GlobalObjectId 文字列を保存
            //   未設定       → どちらも空文字列
            public string RenderSceneCameraAssetGuid = string.Empty;
            public string RenderSceneCameraSceneId = string.Empty;

            public string SixSidedXPlusGuid = string.Empty;
            public string SixSidedXMinusGuid = string.Empty;
            public string SixSidedYPlusGuid = string.Empty;
            public string SixSidedYMinusGuid = string.Empty;
            public string SixSidedZPlusGuid = string.Empty;
            public string SixSidedZMinusGuid = string.Empty;
            public string CubemapGuid = string.Empty;

            public bool[]? FaceFlipH;
            public bool[]? FaceFlipV;

            public bool BlurEnable;
            public CubemapBlurAlgorithm BlurAlgorithm;
            public float BlurRoughness_GGX_SpecularIBL;
            public int BlurNumSamples_GGX_SpecularIBL;
            public float BlurRoughness_Gaussian_Bokeh;
            public int BlurNumSamples_Gaussian_Bokeh;

            public int OutputResolution;
            public string ExportPath = string.Empty;
            public OutputLayoutType OutputLayout;
            public float EquirectangularY;
            public bool EquirectangularImportAsCubemap;
            public bool CrossOrStraightImportAsCubemap = true;
            public bool MatcapFillOutside;
        }

        #endregion
    }
}
