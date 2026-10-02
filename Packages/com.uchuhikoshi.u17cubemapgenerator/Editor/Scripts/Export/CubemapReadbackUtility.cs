using System;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     RenderTexture から Texture2D への非同期リードバック（AsyncGPUReadback）および
    ///     同期フォールバック（ReadPixels）と、グラフィックス API 間の UV 座標正規化を担当するユーティリティクラス。
    /// </summary>
    public static class CubemapReadbackUtility
    {
        /// <summary>
        ///     RenderTexture（2D または Cubemap 面）から Texture2D へ非同期にピクセルデータを読み込みます。
        ///     可能であれば AsyncGPUReadback を使用し、非対応環境やエラー発生時は同期的な ReadPixels にフォールバックします。
        ///     <para>
        ///         【座標系・反転仕様の保証】<br />
        ///         グラフィックス API（DirectX, Metal, Vulkan, OpenGL）および読み出し方式（AsyncGPUReadback / ReadPixels）の違いを本メソッド内部で完全に吸収し、
        ///         常に <b>Texture2D の標準 UV 座標系（左下原点、Y軸上向き）に正規化</b> されたデータを <paramref name="destTex" /> に格納して返します。<br />
        ///         ※ 2D RenderTexture 読み出し時は正立した状態で格納されます。Cubemap 面読み出し時は、
        ///         各出力形式（6面個別画像、クロス／ストレート展開等）に応じた正立化・反転処理は各 Exporter 側の責務として実施されます。
        ///     </para>
        /// </summary>
        public static async Awaitable RequestReadbackIntoTextureAsync(
            RenderTexture source,
            Texture2D destTex,
            int faceIndex,
            CancellationToken cancellationToken,
            string logContext = "")
        {
            var isCube = source.dimension == TextureDimension.Cube;

            if (SystemInfo.supportsAsyncGPUReadback)
            {
                var isTimedOut = false;
                var request = isCube
                    ? AsyncGPUReadback.Request(source, 0, 0, source.width, 0, source.height, faceIndex, 1,
                        destTex.graphicsFormat)
                    : AsyncGPUReadback.Request(source, 0, destTex.graphicsFormat);

                var timeoutSeconds = source.width >= 4096 ? 20.0 : 10.0;
                var startTime = EditorApplication.timeSinceStartup;

                while (!request.done)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.WaitForCompletion();
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    if (EditorApplication.timeSinceStartup - startTime > timeoutSeconds)
                    {
                        request.WaitForCompletion();
                        isTimedOut = true;
                        break;
                    }

                    try
                    {
                        await EditorAwaitableUtility.YieldAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        request.WaitForCompletion();
                        throw;
                    }
                }

                if (request.done && !request.hasError)
                {
                    var data = request.GetData<byte>();
                    var destData = destTex.GetRawTextureData<byte>();

                    if (data.Length != destData.Length || data.Length == 0)
                    {
                        throw new InvalidOperationException(
                            $"[U17CubemapGenerator] AsyncGPUReadback buffer size mismatch ({logContext}): " +
                            $"ReadbackData={data.Length} bytes, DestinationData={destData.Length} bytes.");
                    }

                    try
                    {
                        if (!isCube && SystemInfo.graphicsUVStartsAtTop)
                        {
                            var width = source.width;
                            var height = source.height;
                            var rowPitch = data.Length / height;

                            for (var y = 0; y < height; y++)
                            {
                                var srcRowIndex = y * rowPitch;
                                var dstRowIndex = (height - 1 - y) * rowPitch;
                                var srcSlice = data.GetSubArray(srcRowIndex, rowPitch);
                                var dstSlice = destData.GetSubArray(dstRowIndex, rowPitch);
                                srcSlice.CopyTo(dstSlice);
                            }
                        }
                        else
                        {
                            data.CopyTo(destData);
                        }

                        destTex.Apply(false, false);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(
                            $"[U17CubemapGenerator] Failed to copy AsyncGPUReadback buffer ({logContext}): {ex.Message}");
                        throw;
                    }
                }

                var reason = isTimedOut ? $"timed out after {timeoutSeconds}s" : "encountered an error";
                Debug.LogWarning(
                    $"[U17CubemapGenerator] AsyncGPUReadback {reason} ({logContext}). " +
                    "Falling back to synchronous ReadPixels, which may block the main thread.");
            }
            else
            {
                Debug.LogWarning(
                    $"[U17CubemapGenerator] AsyncGPUReadback is not supported on this platform ({logContext}). " +
                    "Falling back to synchronous ReadPixels.");
            }

            var previousActive = RenderTexture.active;
            try
            {
                if (isCube)
                {
                    Graphics.SetRenderTarget(source, 0, (CubemapFace)faceIndex);
                }
                else
                {
                    RenderTexture.active = source;
                }

                destTex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                destTex.Apply(false, false);
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }
    }
}
