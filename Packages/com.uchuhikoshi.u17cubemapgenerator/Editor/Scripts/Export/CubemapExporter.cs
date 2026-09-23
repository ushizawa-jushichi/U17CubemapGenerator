using System;
using System.Threading;
using UnityEngine;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     各種レイアウトへのエクスポート処理を統括し、適切な個別エクスポーターへ処理を振り分けるクラス。
    /// </summary>
    public static class CubemapExporter
    {
        public static async Awaitable<bool> ExportAsync(
            EditorState state,
            Settings settings,
            RenderTexture cubeRT,
            CancellationToken cancellationToken)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (cubeRT == null)
            {
                throw new ArgumentNullException(nameof(cubeRT));
            }

            if (!CubemapPathUtility.TryResolveExportPath(state.ExportPath, state.OutputLayout, out var resolvedPath,
                    out var isProjectAsset, out var errorMessage))
            {
                Debug.LogError($"[CubemapExporter] {errorMessage}");
                return false;
            }

            var originalCubeRT = cubeRT;
            var scaledRT = (RenderTexture)null!;

            try
            {
                if (originalCubeRT.width != state.OutputResolution && state.OutputResolution > 0)
                {
                    scaledRT = CubemapTextureUtility.CreateScaledCubemap(originalCubeRT, state.OutputResolution,
                        settings.MatBlitter);
                    cubeRT = scaledRT;
                }

                return state.OutputLayout switch
                {
                    OutputLayoutType.LegacyCubemap =>
                        await CubemapAssetExporter.ExportAsync(resolvedPath, cubeRT, cancellationToken),

                    OutputLayoutType.CrossHorizontal or
                        OutputLayoutType.CrossVertical or
                        OutputLayoutType.StraightHorizontal or
                        OutputLayoutType.StraightVertical =>
                        await CubemapCrossOrStraightExporter.ExportAsync(resolvedPath, state.OutputLayout,
                            state.CrossOrStraightImportAsCubemap, cubeRT, isProjectAsset, cancellationToken),

                    OutputLayoutType.Equirectangular =>
                        await CubemapEquirectangularExporter.ExportAsync(resolvedPath, state.EquirectangularY,
                            state.EquirectangularImportAsCubemap, settings.MatBlitter, cubeRT, isProjectAsset,
                            cancellationToken),

                    OutputLayoutType.SixSided =>
                        await CubemapSixSidedExporter.ExportAsync(resolvedPath,
                            Settings.ExportSixSidedSuffixes, cubeRT, isProjectAsset, cancellationToken),

                    OutputLayoutType.Matcap =>
                        await CubemapMatcapExporter.ExportAsync(resolvedPath, state.MatcapFillOutside,
                            settings.MatBlitter, cubeRT, isProjectAsset, cancellationToken),

                    _ => throw new NotSupportedException($"[CubemapExporter] Unsupported layout: {state.OutputLayout}")
                };
            }
            finally
            {
                if (scaledRT != null)
                {
                    RenderTexture.ReleaseTemporary(scaledRT);
                }
            }
        }
    }
}
