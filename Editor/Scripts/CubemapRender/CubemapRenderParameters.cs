using System;
using UnityEngine;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     キューブマップレンダリングおよびブラー処理に必要なパラメータのスナップショット（不変値）。
    ///     非同期実行中の UI 操作やパラメータ変更による途中の混入・不整合を防ぐために使用する。
    /// </summary>
    public readonly struct CubemapRenderParameters
    {
        public readonly InputModeType InputMode;
        public readonly int OutputResolution;
        public readonly bool RenderSceneRotatable;
        public readonly bool RenderSceneHDR;
        public readonly bool RenderSceneSRGB;

        // SixSided 用テクスチャ
        public readonly Texture2D SixSidedXPlus;
        public readonly Texture2D SixSidedXMinus;
        public readonly Texture2D SixSidedYPlus;
        public readonly Texture2D SixSidedYMinus;
        public readonly Texture2D SixSidedZPlus;
        public readonly Texture2D SixSidedZMinus;

        // Cubemap アセット用
        public readonly Cubemap CubemapAsset;

        // ブラー設定
        public readonly bool BlurEnable;
        public readonly CubemapBlurAlgorithm BlurAlgorithm;
        public readonly float BlurRoughness;
        public readonly int BlurNumSamples;
        public readonly int MaxInteractiveBlurResolution;
        public readonly int MaxInteractiveBlurNumSamples;
        public readonly bool IsOverrideResolution;

        // フリップ設定
        public readonly FaceFlipParameters FaceFlips;

        public CubemapRenderParameters(
            InputModeType inputMode,
            int outputResolution,
            bool renderSceneRotatable,
            bool renderSceneHDR,
            bool renderSceneSRGB,
            Texture2D sixSidedXPlus,
            Texture2D sixSidedXMinus,
            Texture2D sixSidedYPlus,
            Texture2D sixSidedYMinus,
            Texture2D sixSidedZPlus,
            Texture2D sixSidedZMinus,
            Cubemap cubemapAsset,
            bool blurEnable,
            CubemapBlurAlgorithm blurAlgorithm,
            float blurRoughness,
            int blurNumSamples,
            int maxInteractiveBlurResolution = 256,
            int maxInteractiveBlurNumSamples = 32,
            bool isOverrideResolution = false,
            FaceFlipParameters faceFlips = default)
        {
            InputMode = inputMode;
            OutputResolution = outputResolution;
            RenderSceneRotatable = renderSceneRotatable;
            RenderSceneHDR = renderSceneHDR;
            RenderSceneSRGB = renderSceneSRGB;

            SixSidedXPlus = sixSidedXPlus;
            SixSidedXMinus = sixSidedXMinus;
            SixSidedYPlus = sixSidedYPlus;
            SixSidedYMinus = sixSidedYMinus;
            SixSidedZPlus = sixSidedZPlus;
            SixSidedZMinus = sixSidedZMinus;

            CubemapAsset = cubemapAsset;

            BlurEnable = blurEnable;
            BlurAlgorithm = blurAlgorithm;
            BlurRoughness = blurRoughness;
            BlurNumSamples = blurNumSamples;
            MaxInteractiveBlurResolution = maxInteractiveBlurResolution;
            MaxInteractiveBlurNumSamples = maxInteractiveBlurNumSamples;
            IsOverrideResolution = isOverrideResolution;

            FaceFlips = faceFlips;
        }

        public static CubemapRenderParameters CreateSnapshot(EditorState state, Settings? settings = null,
            int overrideResolution = 0)
        {
            var roughness = state.BlurAlgorithm == CubemapBlurAlgorithm.GGX_SpecularIBL
                ? state.BlurRoughness_GGX_SpecularIBL
                : state.BlurRoughness_Gaussian_Bokeh;
            var numSamples = state.BlurAlgorithm == CubemapBlurAlgorithm.GGX_SpecularIBL
                ? state.BlurNumSamples_GGX_SpecularIBL
                : state.BlurNumSamples_Gaussian_Bokeh;

            var resolution = overrideResolution > 0 ? overrideResolution : state.OutputResolution;
            var maxBlurRes = settings != null ? settings.MaxInteractiveBlurResolution : 256;
            var maxBlurSamples = settings != null ? settings.MaxInteractiveBlurNumSamples : 32;

            return new CubemapRenderParameters(
                state.InputMode,
                resolution,
                state.RenderSceneRotatable,
                state.RenderSceneHDR,
                state.RenderSceneSRGB,
                state.SixSidedXPlus,
                state.SixSidedXMinus,
                state.SixSidedYPlus,
                state.SixSidedYMinus,
                state.SixSidedZPlus,
                state.SixSidedZMinus,
                state.Cubemap,
                state.BlurEnable,
                state.BlurAlgorithm,
                roughness,
                numSamples,
                maxBlurRes,
                maxBlurSamples,
                overrideResolution > 0,
                FaceFlipParameters.Create(state)
            );
        }

        public bool IsBlurNeeded(RenderTexture sourceTexture)
        {
            if (sourceTexture == null)
            {
                return false;
            }

            return CubemapBlurUtility.IsBlurApplicable(BlurEnable, BlurRoughness, BlurNumSamples);
        }
    }

    /// <summary>
    ///     Cubemap 各面（6面）の水平/垂直反転フラグを保持するパラメータ構造体。
    ///     12ビットのマスク（0..5: Hフリップ, 6..11: Vフリップ）としてアロケーションなしで保持・比較します。
    /// </summary>
    public readonly struct FaceFlipParameters : IEquatable<FaceFlipParameters>
    {
        public int RawMask { get; }

        public FaceFlipParameters(int rawMask)
        {
            RawMask = rawMask;
        }

        public static FaceFlipParameters Create(EditorState? state)
        {
            if (state == null)
            {
                return default;
            }

            var mask = 0;
            for (var i = 0; i < 6; i++)
            {
                if (state.GetFaceFlipH(i))
                {
                    mask |= 1 << i;
                }

                if (state.GetFaceFlipV(i))
                {
                    mask |= 1 << (i + 6);
                }
            }

            return new FaceFlipParameters(mask);
        }

        public bool GetFlipH(int faceIndex)
        {
            return faceIndex is >= 0 and < 6 && (RawMask & (1 << faceIndex)) != 0;
        }

        public bool GetFlipV(int faceIndex)
        {
            return faceIndex is >= 0 and < 6 && (RawMask & (1 << (faceIndex + 6))) != 0;
        }

        public bool HasAnyFlip => RawMask != 0;

        public bool Equals(FaceFlipParameters other)
        {
            return RawMask == other.RawMask;
        }

        public override bool Equals(object? obj)
        {
            return obj is FaceFlipParameters other && Equals(other);
        }

        public override int GetHashCode()
        {
            return RawMask;
        }

        public static bool operator ==(FaceFlipParameters left, FaceFlipParameters right)
        {
            return left.RawMask == right.RawMask;
        }

        public static bool operator !=(FaceFlipParameters left, FaceFlipParameters right)
        {
            return left.RawMask != right.RawMask;
        }
    }
}
