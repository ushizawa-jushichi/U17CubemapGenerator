//#define NO_BUILTIN
//#define ENABLE_URP_MULTI_QUALITY
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

#nullable enable

namespace Uchuhikoshi.RenderPipelineSwitcher
{
	[InitializeOnLoad]
	public static class RenderPipelineSwitcher
	{
#if UNITY_2019_1_OR_NEWER
		static readonly string RenderPipelineSwitcherAsset = "RenderPipelineSwitcherSettingsAsset";

		static RenderPipelineSwitcherSettings? s_settings;

		static RenderPipelineSwitcher()
		{
			EditorApplication.delayCall += () => UpdateMenuCheck();
		}

		static void InitSettings()
		{
			if (s_settings == null)
			{
				string[] guids = AssetDatabase.FindAssets(RenderPipelineSwitcherAsset, null);
				if (guids.Length > 0) 
				{
					var tmp = AssetDatabase.GUIDToAssetPath(guids[0]);
					s_settings = AssetDatabase.LoadAssetAtPath<RenderPipelineSwitcherSettings>(tmp);
				}
			}
		}

		static void SetRenderPipelineAsset(RenderPipelineAsset? rpAsset)
		{
#if UNITY_2022_3_OR_NEWER
			GraphicsSettings.defaultRenderPipeline = rpAsset;
#else
			GraphicsSettings.renderPipelineAsset = rpAsset;
#endif
		    QualitySettings.renderPipeline = rpAsset;
			EditorApplication.delayCall += () => UpdateMenuCheck();
		}

		static RenderPipelineAsset? GetCurrentRenderPipelineAsset()
		{ 
#if UNITY_2022_3_OR_NEWER
			var rpAsset = GraphicsSettings.defaultRenderPipeline;
#else
			var rpAsset = GraphicsSettings.renderPipelineAsset;
#endif
			if (rpAsset == null ) 
			{
				rpAsset = QualitySettings.renderPipeline;
			}
			return rpAsset;
		}

#if !NO_BUILTIN
		[MenuItem("Tools/Switch RenderPipeline/Builtin", false, 1)]
		static void SwitchToBuiltin()
		{
			SetRenderPipelineAsset(null);
		}

		[MenuItem("Tools/Switch RenderPipeline/Builtin", validate = true)]
		static bool SwitchToBuiltinValidate()
		{
			return !Application.isPlaying;
		}
#endif

#if USING_HDRP
		[MenuItem("Tools/Switch RenderPipeline/HDRP", false, 1)]
		static void SwitchToHDRP()
		{
			InitSettings();
			if (s_settings == null) { throw new InvalidOperationException(); }

			SetRenderPipelineAsset(s_settings.hdrpAsset);
		}

		[MenuItem("Tools/Switch RenderPipeline/HDRP", validate = true)]
		static bool SwitchToHDRPValidate()
		{
			return !Application.isPlaying;
		}
#endif

#if USING_URP
#if !ENABLE_URP_MULTI_QUALITY
		[MenuItem("Tools/Switch RenderPipeline/URP", false, 1)]
		static void SwitchToURP()
		{
			InitSettings();
			if (s_settings == null) { throw new InvalidOperationException(); }

			GraphicsSettings.renderPipelineAsset = s_settings.urpAsset;
			QualitySettings.renderPipeline = s_settings.urpAsset;
			EditorApplication.delayCall += () => UpdateMenuCheck();
		}

		[MenuItem("Tools/Switch RenderPipeline/URP", validate = true)]
		static bool SwitchToURPValidate()
		{
			return !Application.isPlaying;
		}
#else

		[MenuItem("Tools/Switch RenderPipeline/URP-PC", validate = true)]
		static bool SwitchToURPPCValidate()
		{
			return !Application.isPlaying;
		}

		[MenuItem("Tools/Switch RenderPipeline/URP-PC", false, 1)]
		static void SwitchToURPPC()
		{
			InitSettings();
			if (s_settings == null) { throw new InvalidOperationException(); }

			SetRenderPipelineAsset(s_settings.urpAssetPC);
		}

		[MenuItem("Tools/Switch RenderPipeline/URP-Mobile", validate = true)]
		static bool SwitchToURPMobileValidate()
		{
			return !Application.isPlaying;
		}

		[MenuItem("Tools/Switch RenderPipeline/URP-Mobile", false, 1)]
		static void SwitchToURPMobile()
		{
			InitSettings();
			if (s_settings == null) { throw new InvalidOperationException(); }

			SetRenderPipelineAsset(s_settings.urpAssetMobile);
		}

#endif

#endif

		static void UpdateMenuCheck()
		{
			var pipelineType = RenderPipelineUtility.DetectPipeline();
#if !NO_BUILTIN
			Menu.SetChecked("Tools/Switch RenderPipeline/Builtin", pipelineType == RenderPipelineUtility.PipelineType.BuiltInPipeline);
#endif
#if USING_HDRP
			Menu.SetChecked("Tools/Switch RenderPipeline/HDRP", pipelineType == RenderPipelineUtility.PipelineType.HDPipeline);
#endif
#if USING_URP
#if !ENABLE_URP_MULTI_QUALITY
			Menu.SetChecked("Tools/Switch RenderPipeline/URP", pipelineType == RenderPipelineUtility.PipelineType.UniversalPipeline);
#else
			InitSettings();
			if (s_settings == null)
			{
				return;
			}
			var rpAsset = GetCurrentRenderPipelineAsset();
			Menu.SetChecked("Tools/Switch RenderPipeline/URP-PC", (pipelineType == RenderPipelineUtility.PipelineType.UniversalPipeline) && (s_settings.urpAssetPC == rpAsset));
			Menu.SetChecked("Tools/Switch RenderPipeline/URP-Mobile", (pipelineType == RenderPipelineUtility.PipelineType.UniversalPipeline) && (s_settings.urpAssetMobile == rpAsset));
#endif
#endif
		}


#endif
	}
}