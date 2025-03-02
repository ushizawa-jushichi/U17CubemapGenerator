using UnityEngine;
using UnityEngine.Rendering;
#if USING_URP
using UnityEngine.Rendering.Universal;
#endif
using System.Reflection;

#nullable enable

namespace Uchuhikoshi
{ 
	public static class RenderPipelineUtility
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

#if USING_URP || USING_HDRP
		public static ScriptableRendererData? GetCurrentScriptableRendererData()
		{
			var rpAsset = GetCurrentRenderPipelineAsset();
			if (rpAsset != null)
			{
				var propertyInfo = rpAsset?.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
				var pipelineObject = propertyInfo?.GetValue(rpAsset);
				if (pipelineObject != null)
				{
					return ((ScriptableRendererData[])pipelineObject)[0];
				}
			}
			return null;
		}
#endif

#if USING_URP
		public static UniversalRenderPipelineAsset? GetCurrentUniversalPipelineAsset()
		{
			return GetCurrentRenderPipelineAsset() as UniversalRenderPipelineAsset;
		}

		public static UniversalRendererData? GetCurrentUniversalRendererData()
		{
			var urpAsset = GetCurrentUniversalPipelineAsset();
			if (urpAsset != null)
			{
#if UNITY_2022_1_OR_NEWER
				return urpAsset.rendererDataList[0] as UniversalRendererData;
#else
				var propertyInfo = urpAsset?.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
				var pipelineObject = propertyInfo?.GetValue(urpAsset);
				if (pipelineObject != null)
				{
					return ((ScriptableRendererData[])pipelineObject)[0] as UniversalRendererData;
				}
#endif


			}
			return null;
		}
#endif

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
