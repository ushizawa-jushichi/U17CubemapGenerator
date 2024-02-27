using UnityEngine;
using UnityEngine.Rendering;

#nullable enable

namespace Uchuhikoshi
{ 
	public class RenderPipelineUtils : MonoBehaviour
	{
		public enum PipelineType
		{
			Unsupported,
			BuiltInPipeline,
#if USING_URP 
			UniversalPipeline,
#endif
#if USING_HDRP 
			HDPipeline
#endif
		}

		public static RenderPipelineAsset? GetCurrentRenderPipelineAsset()
		{
			if (QualitySettings.renderPipeline != null)
			{
				return QualitySettings.renderPipeline;
			}
#if UNITY_2019_1_OR_NEWER
#if UNITY_2022_3_OR_NEWER
			return GraphicsSettings.defaultRenderPipeline;
#else
			return GraphicsSettings.renderPipelineAsset;
#endif
		}

		public static PipelineType DetectPipeline()
		{
			var rpAsset = GetCurrentRenderPipelineAsset();
			if (rpAsset != null)
			{
				var srpType = rpAsset.GetType().ToString();
#if USING_HDRP 
				if (srpType.Contains("HDRenderPipelineAsset"))
				{
					return PipelineType.HDPipeline;
				}
				else 
#endif
#if USING_URP 
				if (srpType.Contains("UniversalRenderPipelineAsset") || srpType.Contains("LightweightRenderPipelineAsset"))
				{
					return PipelineType.UniversalPipeline;
				}
				else
#endif
				{
					return PipelineType.Unsupported;
				}
			}
#elif UNITY_2017_1_OR_NEWER
			if (GraphicsSettings.renderPipelineAsset != null)
			{
				return PipelineType.Unsupported;
			}
#endif
			return PipelineType.BuiltInPipeline;
		}
	}
}
