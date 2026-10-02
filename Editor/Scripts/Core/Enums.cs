namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>プレビュー時に表示する3Dオブジェクトの種別。</summary>
    public enum PreviewObjectType
    {
        Skybox = 0,
        Sphere = 1,
        Cube = 2
    }

    /// <summary>キューブマップの生成元入力ソース種別。</summary>
    public enum InputModeType
    {
        CurrentScene,
        SixSided,
        Cubemap
    }

    /// <summary>キューブマップのエクスポート出力レイアウト形式。</summary>
    public enum OutputLayoutType
    {
        CrossHorizontal,
        CrossVertical,
        StraightHorizontal,
        StraightVertical,
        Equirectangular,
        LegacyCubemap,
        SixSided,
        Matcap,
        Octahedral
    }

    /// <summary>キューブマップブラーの計算アルゴリズム。</summary>
    public enum CubemapBlurAlgorithm
    {
        GGX_SpecularIBL = 0,
        Gaussian_Bokeh = 1
    }
}
