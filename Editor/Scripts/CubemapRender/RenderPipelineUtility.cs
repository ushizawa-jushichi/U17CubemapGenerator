using UnityEngine;
using UnityEngine.Rendering;
#if U17_HDRP_SUPPORT
using UnityEngine.Rendering.HighDefinition;
#endif
#if U17_URP_SUPPORT
using UnityEngine.Rendering.Universal;
#endif

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     Universal Render Pipeline (URP) および High Definition Render Pipeline (HDRP) のアクティブ状態を判定するユーティリティ。
    /// </summary>
    public static class RenderPipelineUtility
    {
        /// <summary>
        ///     現在のプロジェクトで Universal Render Pipeline (URP) が有効に設定されているかを判定します。
        /// </summary>
        public static bool IsUniversalRenderPipelineActive()
        {
            var pipeline = QualitySettings.renderPipeline != null
                ? QualitySettings.renderPipeline
                : GraphicsSettings.currentRenderPipeline;

            if (pipeline == null)
            {
                return false;
            }

#if U17_URP_SUPPORT
            if (pipeline is UniversalRenderPipelineAsset)
            {
                return true;
            }
#endif
            return pipeline.GetType().Name.Contains("UniversalRenderPipelineAsset");
        }

        /// <summary>
        ///     現在のプロジェクトで High Definition Render Pipeline (HDRP) が有効に設定されているかを判定します。
        /// </summary>
        public static bool IsHighDefinitionRenderPipelineActive()
        {
            var pipeline = QualitySettings.renderPipeline != null
                ? QualitySettings.renderPipeline
                : GraphicsSettings.currentRenderPipeline;

            if (pipeline == null)
            {
                return false;
            }

#if U17_HDRP_SUPPORT
            if (pipeline is HDRenderPipelineAsset)
            {
                return true;
            }
#endif
            return pipeline.GetType().Name.Contains("HDRenderPipelineAsset");
        }

        /// <summary>
        ///     現在のプロジェクトで URP または HDRP が有効に設定されているかを判定します。
        /// </summary>
        public static bool IsSupportedPipelineActive()
        {
            return IsUniversalRenderPipelineActive() || IsHighDefinitionRenderPipelineActive();
        }
    }
}
