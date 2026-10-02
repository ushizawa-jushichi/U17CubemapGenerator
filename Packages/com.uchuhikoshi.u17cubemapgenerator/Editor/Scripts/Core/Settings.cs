using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     U17CubemapGenerator のデフォルト設定やシェーダー・メッシュ等のアセット参照を保持する ScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "U17CubemapGeneratorSettings",
        menuName = "Tools/U17CubemapGenerator/U17CubemapGeneratorSettings")]
    public sealed class Settings : ScriptableObject
    {
        public static readonly string[] ExportSixSidedSuffixes =
            { "_XPlus", "_XMinus", "_YPlus", "_YMinus", "_ZPlus", "_ZMinus" };

        public static readonly int[] Resolutions = { 64, 128, 256, 512, 1024, 2048, 4096 };

        [SerializeField] private StyleSheet _styleUSS = null!;

        [SerializeField] private ComputeShader _cubemapBlurShader = null!;

        [SerializeField] private Mesh _meshIcosphere = null!;

        [SerializeField] private Mesh _meshSkybox = null!;

        [SerializeField] private Material _matPreview = null!;

        [SerializeField] private Material _matBlitter = null!;

        [SerializeField] private Material _matPreviewHDRP = null!;

        [SerializeField] private Material _matBlitterHDRP = null!;

        [SerializeField] private Material _matSkybox = null!;

        [SerializeField] private float _dragSpeed = 1.0f;

        [SerializeField] private int _redrawDebounceTimeMs = 1;

        [SerializeField] private int _renderDebounceTimeMs = 33;

        [SerializeField] private int _blurDebounceTimeMs = 33;

        [SerializeField] private int _resizeDebounceTimeMs = 150;

        [Header("Camera Tracking")] [SerializeField]
        private double _cameraTrackingIdlePollInterval = 0.2;

        [SerializeField] private double _cameraTrackingPlayModePollInterval = 0.033;

        [SerializeField] private float _cameraTrackingPositionThreshold = 0.0001f;

        [SerializeField] private float _cameraTrackingRotationAngleThreshold = 0.01f;

        [Header("Camera Fallback")] [SerializeField]
        private float _defaultCameraNearClipPlane = 0.3f;

        [SerializeField] private float _defaultCameraFarClipPlane = 1000f;

        [SerializeField] private bool _defaultHideUI;

        [SerializeField] private bool _defaultHidePreview;

        [SerializeField] private PreviewObjectType _defaultPreviewObject = PreviewObjectType.Sphere;

        [SerializeField] private bool _defaultSuperSampling;

        [SerializeField] private bool _defaultPreviewBgSkybox = true;

        [SerializeField] private Vector3 _defaultPreviewRotation = new(0f, 0f, 0f);

        [SerializeField] private bool _defaultPreviewRotationXEnable = true;

        [SerializeField] private bool _defaultPreviewRotationYEnable = true;

        [SerializeField] private bool _defaultPreviewRotationZEnable = true;

        [SerializeField] private InputModeType _defaultInputMode = InputModeType.CurrentScene;

        [SerializeField] private bool _defaultRenderSceneSyncSceneViewCamera;

        [SerializeField] private bool _defaultRenderSceneRotatable = true;

        [SerializeField] private bool _defaultRenderSceneHDR;

        [SerializeField] private bool _defaultRenderSceneSRGB = true;

        [SerializeField] private Texture2D _defaultSixSidedXPlus = null!;

        [SerializeField] private Texture2D _defaultSixSidedXMinus = null!;

        [SerializeField] private Texture2D _defaultSixSidedYPlus = null!;

        [SerializeField] private Texture2D _defaultSixSidedYMinus = null!;

        [SerializeField] private Texture2D _defaultSixSidedZPlus = null!;

        [SerializeField] private Texture2D _defaultSixSidedZMinus = null!;

        [SerializeField] private Cubemap _defaultCubemap = null!;

        [SerializeField] private bool _defaultBlurEnable = true;

        [SerializeField] private CubemapBlurAlgorithm _defaultBlurAlgorithm = CubemapBlurAlgorithm.GGX_SpecularIBL;

        [SerializeField] private float _defaultBlurRoughness_GGX_SpecularIBL = 0.15f;

        [SerializeField] private int _defaultBlurNumSamples_GGX_SpecularIBL = 128;

        [SerializeField] private float _defaultBlurRoughness_Gaussian_Bokeh = 0.15f;

        [SerializeField] private int _defaultBlurNumSamples_Gaussian_Bokeh = 128;

        [Header("Interactive Blur Limits")] [SerializeField]
        private int _maxInteractiveBlurResolution = 256;

        [SerializeField] private int _maxInteractiveBlurNumSamples = 64;

        [SerializeField] private int _defaultOutputResolution = 1024;

        [SerializeField] private string _defaultExportPath = "Assets/GeneratedCubemap.png";

        [SerializeField] private OutputLayoutType _defaultOutputLayout = OutputLayoutType.CrossHorizontal;

        [SerializeField] private float _defaultEquirectangularY;

        [SerializeField] private bool _defaultEquirectangularImportAsCubemap;

        [SerializeField] private bool _defaultCrossOrStraightImportAsCubemap = true;

        [SerializeField] private bool _defaultMatcapFillOutside = true;

        [Header("Six-Sided Drop Suffixes")] [SerializeField]
        private string[] _dropSuffixesXPlus =
            { "_xplus", "_posx", "_px", "+x", "_right", "_rt", "-right", "-rt", "-px" };

        [SerializeField] private string[] _dropSuffixesXMinus =
            { "_xminus", "_negx", "_nx", "-x", "_left", "_lf", "-left", "-lf", "-nx" };

        [SerializeField] private string[] _dropSuffixesYPlus =
            { "_yplus", "_posy", "_py", "+y", "_up", "_top", "-up", "-top", "-py" };

        [SerializeField] private string[] _dropSuffixesYMinus =
            { "_yminus", "_negy", "_ny", "-y", "_down", "_bottom", "_dn", "_btm", "-down", "-bottom", "-ny" };

        [SerializeField] private string[] _dropSuffixesZPlus =
            { "_zplus", "_posz", "_pz", "+z", "_front", "_forward", "_ft", "_fwd", "-front", "-ft", "-pz" };

        [SerializeField] private string[] _dropSuffixesZMinus =
            { "_zminus", "_negz", "_nz", "-z", "_back", "_backward", "_bk", "_bwd", "-back", "-bk", "-nz" };

        public StyleSheet StyleUSS => _styleUSS;
        public ComputeShader CubemapBlurShader => _cubemapBlurShader;
        public Mesh MeshIcosphere => _meshIcosphere;
        public Mesh MeshSkybox => _meshSkybox;

        public Material MatPreview
        {
            get
            {
                if (RenderPipelineUtility.IsHighDefinitionRenderPipelineActive())
                {
                    if (_matPreviewHDRP != null && _matPreviewHDRP.shader != null &&
                        _matPreviewHDRP.shader.name != "Hidden/InternalErrorShader")
                    {
                        return _matPreviewHDRP;
                    }

                    Debug.LogError(
                        "[U17CubemapGenerator] MatPreviewHDRP is missing or has an invalid shader in HDRP environment. Please assign a valid HDRP Preview Material in Settings asset.");
                    return null!;
                }

                return _matPreview;
            }
        }

        public Material MatBlitter
        {
            get
            {
                if (RenderPipelineUtility.IsHighDefinitionRenderPipelineActive())
                {
                    if (_matBlitterHDRP != null && _matBlitterHDRP.shader != null &&
                        _matBlitterHDRP.shader.name != "Hidden/InternalErrorShader")
                    {
                        return _matBlitterHDRP;
                    }

                    Debug.LogError(
                        "[U17CubemapGenerator] MatBlitterHDRP is missing or has an invalid shader in HDRP environment. Please assign a valid HDRP Blitter Material in Settings asset.");
                    return null!;
                }

                return _matBlitter;
            }
        }

        public Material MatSkybox => _matSkybox;
        public float DragSpeed => _dragSpeed;
        public int RedrawDebounceTimeMs => _redrawDebounceTimeMs;
        public int RenderDebounceTimeMs => _renderDebounceTimeMs;
        public int BlurDebounceTimeMs => _blurDebounceTimeMs;
        public int ResizeDebounceTimeMs => _resizeDebounceTimeMs;

        public double CameraTrackingIdlePollInterval => _cameraTrackingIdlePollInterval;
        public double CameraTrackingPlayModePollInterval => _cameraTrackingPlayModePollInterval;
        public float CameraTrackingPositionThreshold => _cameraTrackingPositionThreshold;
        public float CameraTrackingRotationAngleThreshold => _cameraTrackingRotationAngleThreshold;

        public float DefaultCameraNearClipPlane => _defaultCameraNearClipPlane;
        public float DefaultCameraFarClipPlane => _defaultCameraFarClipPlane;

        public bool DefaultHideUI => _defaultHideUI;
        public bool DefaultHidePreview => _defaultHidePreview;
        public PreviewObjectType DefaultPreviewObject => _defaultPreviewObject;
        public bool DefaultSuperSampling => _defaultSuperSampling;
        public bool DefaultPreviewBgSkybox => _defaultPreviewBgSkybox;
        public Vector3 DefaultPreviewRotation => _defaultPreviewRotation;
        public bool DefaultPreviewRotationXEnable => _defaultPreviewRotationXEnable;
        public bool DefaultPreviewRotationYEnable => _defaultPreviewRotationYEnable;
        public bool DefaultPreviewRotationZEnable => _defaultPreviewRotationZEnable;

        public InputModeType DefaultInputMode => _defaultInputMode;
        public bool DefaultRenderSceneSyncSceneViewCamera => _defaultRenderSceneSyncSceneViewCamera;
        public bool DefaultRenderSceneRotatable => _defaultRenderSceneRotatable;
        public bool DefaultRenderSceneHDR => _defaultRenderSceneHDR;
        public bool DefaultRenderSceneSRGB => _defaultRenderSceneSRGB;
        public Texture2D DefaultSixSidedXPlus => _defaultSixSidedXPlus;
        public Texture2D DefaultSixSidedXMinus => _defaultSixSidedXMinus;
        public Texture2D DefaultSixSidedYPlus => _defaultSixSidedYPlus;
        public Texture2D DefaultSixSidedYMinus => _defaultSixSidedYMinus;
        public Texture2D DefaultSixSidedZPlus => _defaultSixSidedZPlus;
        public Texture2D DefaultSixSidedZMinus => _defaultSixSidedZMinus;
        public Cubemap DefaultCubemap => _defaultCubemap;

        public bool DefaultBlurEnable => _defaultBlurEnable;
        public CubemapBlurAlgorithm DefaultBlurAlgorithm => _defaultBlurAlgorithm;
        public float DefaultBlurRoughness_GGX_SpecularIBL => _defaultBlurRoughness_GGX_SpecularIBL;
        public int DefaultBlurNumSamples_GGX_SpecularIBL => _defaultBlurNumSamples_GGX_SpecularIBL;
        public float DefaultBlurRoughness_Gaussian_Bokeh => _defaultBlurRoughness_Gaussian_Bokeh;
        public int DefaultBlurNumSamples_Gaussian_Bokeh => _defaultBlurNumSamples_Gaussian_Bokeh;

        public int MaxInteractiveBlurResolution => _maxInteractiveBlurResolution;
        public int MaxInteractiveBlurNumSamples => _maxInteractiveBlurNumSamples;

        public int DefaultOutputResolution => _defaultOutputResolution;
        public string DefaultExportPath => _defaultExportPath;
        public OutputLayoutType DefaultOutputLayout => _defaultOutputLayout;
        public float DefaultEquirectangularY => _defaultEquirectangularY;
        public bool DefaultEquirectangularImportAsCubemap => _defaultEquirectangularImportAsCubemap;
        public bool DefaultCrossOrStraightImportAsCubemap => _defaultCrossOrStraightImportAsCubemap;
        public bool DefaultMatcapFillOutside => _defaultMatcapFillOutside;

        /// <summary>
        ///     テクスチャ名から対応するキューブマップの面インデックス（0: X+, 1: X-, 2: Y+, 3: Y-, 4: Z+, 5: Z-）を判定します。
        ///     合致するサフィックスがない場合は -1 を返します。
        /// </summary>
        public int ResolveSixSidedFaceIndex(string textureName)
        {
            if (string.IsNullOrEmpty(textureName))
            {
                return -1;
            }

            var lower = textureName.ToLowerInvariant();

            if (MatchesAnySuffix(lower, _dropSuffixesXPlus))
            {
                return 0;
            }

            if (MatchesAnySuffix(lower, _dropSuffixesXMinus))
            {
                return 1;
            }

            if (MatchesAnySuffix(lower, _dropSuffixesYPlus))
            {
                return 2;
            }

            if (MatchesAnySuffix(lower, _dropSuffixesYMinus))
            {
                return 3;
            }

            if (MatchesAnySuffix(lower, _dropSuffixesZPlus))
            {
                return 4;
            }

            if (MatchesAnySuffix(lower, _dropSuffixesZMinus))
            {
                return 5;
            }

            return -1;
        }

        private static bool MatchesAnySuffix(string name, string[] suffixes)
        {
            if (suffixes == null)
            {
                return false;
            }

            for (var i = 0; i < suffixes.Length; i++)
            {
                var suffix = suffixes[i];
                if (!string.IsNullOrEmpty(suffix) && name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static Settings CreateNewSettingInstance(string assetPath, bool saveToAsset = true)
        {
            var assetFolder = Path.GetDirectoryName(assetPath) ?? string.Empty;
            var settings = CreateInstance<Settings>();
            settings._styleUSS = LoadRequiredAsset<StyleSheet>(assetFolder, "Style.uss");
            settings._cubemapBlurShader =
                LoadRequiredAsset<ComputeShader>(assetFolder, "Shaders/CubemapBlur.compute");
            settings._meshIcosphere = LoadRequiredAsset<Mesh>(assetFolder, "mesh_icosphere_sub5.asset");
            settings._meshSkybox = LoadRequiredAsset<Mesh>(assetFolder, "mesh_cube_reverse.asset");
            settings._matPreview = LoadRequiredAsset<Material>(assetFolder, "Materials/PreviewURP_Mat.mat");
            settings._matBlitter = LoadRequiredAsset<Material>(assetFolder, "Materials/BlitterURP_Mat.mat");
            settings._matPreviewHDRP = LoadOptionalAsset<Material>(assetFolder, "Materials/PreviewHDRP_Mat.mat")!;
            settings._matBlitterHDRP = LoadOptionalAsset<Material>(assetFolder, "Materials/BlitterHDRP_Mat.mat")!;
            settings._matSkybox = LoadRequiredAsset<Material>(assetFolder, "Materials/Skybox_Mat.mat");

            if (saveToAsset && !string.IsNullOrEmpty(assetPath))
            {
                try
                {
                    var fullPath = Path.GetFullPath(assetPath);
                    var dir = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    AssetDatabase.CreateAsset(settings, assetPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log(
                        $"[CubemapGeneratorSettings] Successfully created and saved settings asset at '{assetPath}'.");
                    return settings;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[CubemapGeneratorSettings] Could not save settings asset to '{assetPath}'. Falling back to in-memory instance. Reason: {ex.Message}");
                }
            }

            settings.hideFlags = HideFlags.DontSave;
            return settings;
        }

        private static T LoadRequiredAsset<T>(string folder, string relativePath) where T : Object
        {
            var path = Path.Combine(folder, relativePath).Replace("\\", "/");
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
            {
                throw new FileNotFoundException($"Failed to load a required asset: {path}");
            }

            return asset;
        }

        private static T LoadOptionalAsset<T>(string folder, string relativePath) where T : Object
        {
            var path = Path.Combine(folder, relativePath).Replace("\\", "/");
            return AssetDatabase.LoadAssetAtPath<T>(path)!;
        }
    }
}
