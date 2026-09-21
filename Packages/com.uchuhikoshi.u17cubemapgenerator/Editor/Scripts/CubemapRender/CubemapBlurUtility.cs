using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     Compute Shader を用いてキューブマップにブラー（GGX / Bokeh）を適用するユーティリティ。
    ///     大負荷時にはチャンク分割ディスパッチと非同期フレーム譲渡を行い、GPU の TDR (デバイスエラー) を防止します。
    /// </summary>
    public static class CubemapBlurUtility
    {
        private const int ComputeThreadGroupSize = 8;
        private const int CubemapFaceCount = 6;
        private const long TargetSamplesPerDispatch = 16_000_000L;
        private const double MaxFrameTimeMs = 8.0;

        public static bool IsBlurApplicable(bool blurEnable, float blurRoughness, int blurNumSamples)
        {
            return blurEnable && blurRoughness > 0f && blurNumSamples > 0;
        }


        public static async Awaitable<RenderTexture> GenerateBlurredCubemapAsync(
            RenderTexture sourceRTCube,
            RenderTexture destinationCubeRT,
            RenderTexture workArrayRT,
            float roughness,
            int numSamples,
            CubemapBlurAlgorithm algorithm,
            ComputeShader computeShader,
            IProgress<float>? progress = null,
            CancellationToken cancellationToken = default)
        {
            SetupComputeShader(sourceRTCube, destinationCubeRT, workArrayRT, roughness, numSamples, algorithm,
                computeShader, out var kernel, out var resolution);

            var totalPixelSamples = (long)resolution * resolution * CubemapFaceCount * numSamples;
            var threadGroupsX = Mathf.CeilToInt((float)resolution / ComputeThreadGroupSize);

            // 小規模なワークロードの場合は 1 回で一括ディスパッチ
            if (totalPixelSamples <= TargetSamplesPerDispatch)
            {
                computeShader.SetInt("_FaceIndex", -1);
                computeShader.SetInt("_OffsetY", 0);
                computeShader.SetInt("_BlockHeight", 0);

                var threadGroupsY = Mathf.CeilToInt((float)resolution / ComputeThreadGroupSize);
                computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, CubemapFaceCount);

                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(1.0f);

                CopyAndGenerateMips(workArrayRT, destinationCubeRT);
                return destinationCubeRT;
            }

            // 大規模なワークロードは行ブロック単位（Y軸スライス）でチャンク分割ディスパッチ
            using (new PlayerLoopUpdateScope())
            {
                var samplesPerRow = (long)resolution * numSamples;
                var rawRows = (int)Math.Max(ComputeThreadGroupSize,
                    TargetSamplesPerDispatch / Math.Max(1L, samplesPerRow));
                var rowsPerChunk =
                    Mathf.Clamp(
                        (rawRows + ComputeThreadGroupSize - 1) / ComputeThreadGroupSize * ComputeThreadGroupSize,
                        ComputeThreadGroupSize, resolution);

                var totalChunks = 0;
                for (var f = 0; f < CubemapFaceCount; f++)
                {
                    for (var y = 0; y < resolution; y += rowsPerChunk)
                    {
                        totalChunks++;
                    }
                }

                var processedChunks = 0;
                var stopwatch = Stopwatch.StartNew();

                for (var face = 0; face < CubemapFaceCount; face++)
                {
                    computeShader.SetInt("_FaceIndex", face);

                    for (var offsetY = 0; offsetY < resolution; offsetY += rowsPerChunk)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var blockHeight = Mathf.Min(rowsPerChunk, resolution - offsetY);
                        computeShader.SetInt("_OffsetY", offsetY);
                        computeShader.SetInt("_BlockHeight", blockHeight);

                        var threadGroupsY = Mathf.CeilToInt((float)blockHeight / ComputeThreadGroupSize);
                        computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

                        processedChunks++;
                        progress?.Report((float)processedChunks / totalChunks);

                        // 1フレームあたりの実行時間が制限を超えた場合、次のフレームへ処理を譲渡して TDR を回避
                        if (stopwatch.Elapsed.TotalMilliseconds >= MaxFrameTimeMs)
                        {
                            await EditorAwaitableUtility.YieldAsync(cancellationToken);
                            stopwatch.Restart();
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                CopyAndGenerateMips(workArrayRT, destinationCubeRT);
                return destinationCubeRT;
            }
        }

        private static void SetupComputeShader(
            RenderTexture sourceRTCube,
            RenderTexture destinationCubeRT,
            RenderTexture workArrayRT,
            float roughness,
            int numSamples,
            CubemapBlurAlgorithm algorithm,
            ComputeShader computeShader,
            out int kernel,
            out int resolution)
        {
            Assert.IsNotNull(sourceRTCube, $"{nameof(sourceRTCube)} is null.");
            Assert.IsNotNull(destinationCubeRT, $"{nameof(destinationCubeRT)} is null.");
            Assert.IsNotNull(workArrayRT, $"{nameof(workArrayRT)} is null.");
            Assert.IsNotNull(computeShader, $"{nameof(computeShader)} is null.");

            resolution = destinationCubeRT.width;
            kernel = computeShader.FindKernel("CSMain");
            computeShader.SetTexture(kernel, "_SourceTex", sourceRTCube);
            computeShader.SetTexture(kernel, "_ResultTex", workArrayRT);
            computeShader.SetFloat("_Roughness", roughness);
            computeShader.SetInt("_NumSamples", numSamples);
            computeShader.SetInt("_Resolution", resolution);
            computeShader.SetInt("_Algorithm", (int)algorithm);

            var isHDR = GraphicsFormatUtility.IsHDRFormat(destinationCubeRT.graphicsFormat);
            var isSRGB = destinationCubeRT.sRGB || GraphicsFormatUtility.IsSRGBFormat(destinationCubeRT.graphicsFormat);
            var isLinearSpace = QualitySettings.activeColorSpace == ColorSpace.Linear;
            // コンピュートシェーダーの UAV 書き込みは ROP (ハードウェア sRGB エンコード) を経由しないため、
            // 出力先 destinationCubeRT が sRGB フォーマットである場合は、シェーダー内で手動 LinearToSRGB を適用して
            // ガンマエンコード済みデータをメモリに書き込む必要がある。出力先の設定と完全に連動させる。
            var needsSRGBConversion = !isHDR && isSRGB && isLinearSpace;
            computeShader.SetInt("_IsLinearToSRGB", needsSRGBConversion ? 1 : 0);
        }

        private static void CopyAndGenerateMips(RenderTexture workArrayRT, RenderTexture destinationCubeRT)
        {
            for (var i = 0; i < CubemapFaceCount; i++)
            {
                Graphics.CopyTexture(workArrayRT, i, destinationCubeRT, i);
            }

            if (destinationCubeRT.useMipMap)
            {
                destinationCubeRT.GenerateMips();
            }
        }
    }
}
