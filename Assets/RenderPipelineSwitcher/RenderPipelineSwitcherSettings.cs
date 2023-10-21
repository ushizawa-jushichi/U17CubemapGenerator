//#define ENABLE_URP_MULTI_QUALITY
using UnityEngine;
#if USING_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif
#if USING_URP
using UnityEngine.Rendering.Universal;
#endif

#nullable enable

namespace Ushino17
{
	[CreateAssetMenu(fileName = "RenderPipelineSwitcherSettingsAsset", menuName = "Ushino17/U17CubemapGenerator/Create RenderPipelineSwitcherSettingsAsset")]
	public class RenderPipelineSwitcherSettings : ScriptableObject
	{
#if USING_HDRP
		public HDRenderPipelineAsset? hdrpAsset;
#endif
#if USING_URP
#if !ENABLE_URP_MULTI_QUALITY
		public UniversalRenderPipelineAsset? urpAsset;
#else
		public UniversalRenderPipelineAsset? urpAssetBalanced;
		public UniversalRenderPipelineAsset? urpAssetHighFidelity;
		public UniversalRenderPipelineAsset? urpAssetPerformant;
#endif
#endif
	}
}