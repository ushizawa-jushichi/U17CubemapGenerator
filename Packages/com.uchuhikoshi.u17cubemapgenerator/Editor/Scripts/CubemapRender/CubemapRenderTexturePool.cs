using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     キューブマップレンダリングおよびブラー処理に使用する一時 RenderTexture のキャッシュとライフサイクルを管理するクラス。
    ///     インタラクティブ用（低解像度ドラフト）と通常用（フル解像度）の2スロットに分離することで、
    ///     スライダー操作時のキャッシュ破棄と再生成の ping-pong を防止する。
    /// </summary>
    public sealed class CubemapRenderTexturePool : IDisposable
    {
        private readonly TextureSlot _draftSlot = new();

        private readonly TextureSlot _mainSlot = new();

        public void Dispose()
        {
            _mainSlot.Dispose();
            _draftSlot.Dispose();
        }

        private TextureSlot GetSlot(bool isInteractive)
        {
            return isInteractive ? _draftSlot : _mainSlot;
        }

        public RenderTexture GetPreviewCubeRenderTexture(int cubemapSize, bool isHDR, bool isSRGB, bool useCache,
            bool isInteractive = false)
        {
            var slot = GetSlot(isInteractive);

            if (slot.PreviewCubeRT != null)
            {
                if (useCache &&
                    slot.PreviewCubeSizeCache == cubemapSize &&
                    slot.PreviewCubeHDRCache == isHDR &&
                    slot.PreviewCubeSRGBCache == isSRGB)
                {
                    if (!slot.PreviewCubeRT.IsCreated())
                    {
                        slot.PreviewCubeRT.Create();
                    }

                    return slot.PreviewCubeRT;
                }
            }

            slot.PreviewCubeSizeCache = cubemapSize;
            slot.PreviewCubeHDRCache = isHDR;
            slot.PreviewCubeSRGBCache = isSRGB;

            SafeReleaseAndDestroy(ref slot.PreviewCubeRT!);

            var rtDesc = new RenderTextureDescriptor(cubemapSize, cubemapSize)
            {
                dimension = TextureDimension.Cube,
                colorFormat = isHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32,
                depthBufferBits = 0,
                msaaSamples = 1,
                sRGB = isSRGB,
                useMipMap = true,
                autoGenerateMips = false
            };

            var slotPrefix = isInteractive ? "Draft" : "Main";
            var rt = new RenderTexture(rtDesc)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = $"U17CubeGen.{slotPrefix}PreviewCubeRT_{cubemapSize}x{cubemapSize}"
            };

            if (!rt.IsCreated())
            {
                rt.Create();
            }

            slot.PreviewCubeRT = rt;

            CubemapTextureUtility.ClearAllFaces(slot.PreviewCubeRT);
            return slot.PreviewCubeRT;
        }

        public void GetBlurredRTs(RenderTexture cubeRT, int outputResolution, out RenderTexture previewRTArray,
            out RenderTexture previewBlurredCubeRT, bool isInteractive = false)
        {
            var slot = GetSlot(isInteractive);
            var cubemapSize = outputResolution;
            var isHDR = GraphicsFormatUtility.IsHDRFormat(cubeRT.graphicsFormat);
            var isSRGB = cubeRT.sRGB || GraphicsFormatUtility.IsSRGBFormat(cubeRT.graphicsFormat);

            if (slot.BlurredSizeCache == cubemapSize &&
                slot.BlurredHDRCache == isHDR &&
                slot.BlurredSRGBCache == isSRGB &&
                slot.PreviewRTArray != null && slot.PreviewRTArray.IsCreated() &&
                slot.PreviewRTArray.width == cubemapSize &&
                GraphicsFormatUtility.IsHDRFormat(slot.PreviewRTArray.graphicsFormat) == isHDR &&
                slot.PreviewBlurredCubeRT != null && slot.PreviewBlurredCubeRT.IsCreated() &&
                GraphicsFormatUtility.IsHDRFormat(slot.PreviewBlurredCubeRT.graphicsFormat) == isHDR &&
                (slot.PreviewBlurredCubeRT.sRGB ||
                 GraphicsFormatUtility.IsSRGBFormat(slot.PreviewBlurredCubeRT.graphicsFormat)) == isSRGB)
            {
                previewRTArray = slot.PreviewRTArray;
                previewBlurredCubeRT = slot.PreviewBlurredCubeRT;
                return;
            }

            SafeReleaseAndDestroy(ref slot.PreviewRTArray!);
            SafeReleaseAndDestroy(ref slot.PreviewBlurredCubeRT!);

            var baseDesc = cubeRT.descriptor;
            baseDesc.width = outputResolution;
            baseDesc.height = outputResolution;
            baseDesc.depthBufferBits = 0;
            baseDesc.msaaSamples = 1;
            baseDesc.sRGB = isSRGB;
            baseDesc.useMipMap = cubeRT.useMipMap;
            baseDesc.autoGenerateMips = false;

            var arrayDesc = baseDesc;
            arrayDesc.dimension = TextureDimension.Tex2DArray;
            arrayDesc.volumeDepth = 6;
            arrayDesc.enableRandomWrite = true;
            arrayDesc.sRGB = false;

            var slotPrefix = isInteractive ? "Draft" : "Main";
            var rtArray = new RenderTexture(arrayDesc)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = $"U17CubeGen.{slotPrefix}PreviewRTArray_{outputResolution}x{outputResolution}"
            };
            if (!rtArray.IsCreated())
            {
                rtArray.Create();
            }

            slot.PreviewRTArray = rtArray;

            var cubeDesc = baseDesc;
            cubeDesc.dimension = TextureDimension.Cube;
            cubeDesc.volumeDepth = 1;
            cubeDesc.enableRandomWrite = false;

            var blurredCubeRT = new RenderTexture(cubeDesc)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = $"U17CubeGen.{slotPrefix}PreviewBlurredCubeRT_{outputResolution}x{outputResolution}"
            };
            if (!blurredCubeRT.IsCreated())
            {
                blurredCubeRT.Create();
            }

            slot.PreviewRTArray = rtArray;
            slot.PreviewBlurredCubeRT = blurredCubeRT;
            slot.BlurredSizeCache = cubemapSize;
            slot.BlurredHDRCache = isHDR;
            slot.BlurredSRGBCache = isSRGB;

            previewRTArray = slot.PreviewRTArray;
            previewBlurredCubeRT = slot.PreviewBlurredCubeRT;
        }

        private static void SafeReleaseAndDestroy(ref RenderTexture rt)
        {
            if (rt != null)
            {
                rt.Release();
                Object.DestroyImmediate(rt);
            }

            rt = null!;
        }

        private sealed class TextureSlot : IDisposable
        {
            public bool BlurredHDRCache;
            public int BlurredSizeCache = -1;
            public bool BlurredSRGBCache;
            public RenderTexture PreviewBlurredCubeRT = null!;
            public bool PreviewCubeHDRCache;
            public RenderTexture PreviewCubeRT = null!;
            public int PreviewCubeSizeCache = -1;
            public bool PreviewCubeSRGBCache;

            public RenderTexture PreviewRTArray = null!;

            public void Dispose()
            {
                SafeReleaseAndDestroy(ref PreviewCubeRT!);
                SafeReleaseAndDestroy(ref PreviewBlurredCubeRT!);
                SafeReleaseAndDestroy(ref PreviewRTArray!);

                PreviewCubeSizeCache = -1;
                BlurredSizeCache = -1;
            }
        }
    }
}
