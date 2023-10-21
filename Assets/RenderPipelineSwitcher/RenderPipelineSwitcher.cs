//#define NO_BUILTIN
//#define ENABLE_URP_MULTI_QUALITY
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

#nullable enable

namespace Ushino17
{
	[InitializeOnLoad]
	public static class RenderPipelineSwitcher
	{
#if UNITY_2019_1_OR_NEWER
		static readonly string RenderPipelineSwitcherAsset = "RenderPipelineSwitcherSettingsAsset";

		static RenderPipelineSwitcherSettings? _settings;

		static RenderPipelineSwitcher()
		{
			EditorApplication.delayCall += () => UpdateMenuCheck();
		}

		static void InitSettings()
		{
			if (_settings == null)
			{
				string[] guids = AssetDatabase.FindAssets(RenderPipelineSwitcherAsset, null);
				if (guids.Length > 0) 
				{
					var tmp = AssetDatabase.GUIDToAssetPath(guids[0]);
					_settings = AssetDatabase.LoadAssetAtPath<RenderPipelineSwitcherSettings>(tmp);
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
			if (_settings == null) { throw new InvalidOperationException(); }

			SetRenderPipelineAsset(_settings.hdrpAsset);
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
			if (_settings == null) { throw new InvalidOperationException(); }

			GraphicsSettings.renderPipelineAsset = _settings.urpAsset;
			QualitySettings.renderPipeline = _settings.urpAsset;
			EditorApplication.delayCall += () => UpdateMenuCheck();
		}

		[MenuItem("Tools/Switch RenderPipeline/URP", validate = true)]
		static bool SwitchToURPValidate()
		{
			return !Application.isPlaying;
		}
#else
		[MenuItem("Tools/Switch RenderPipeline/URP-Balanced", false, 1)]
		static void SwitchToURPBalanced()
		{
			InitSettings();
			if (_settings == null) { throw new InvalidOperationException(); }

			SetRenderPipelineAsset(_settings.urpAssetBalanced);
		}

		[MenuItem("Tools/Switch RenderPipeline/URP-Balanced", validate = true)]
		static bool SwitchToURPBalancedValidate()
		{
			return !Application.isPlaying;
		}

		[MenuItem("Tools/Switch RenderPipeline/URP-HighFidelity", false, 1)]
		static void SwitchToURPHighFidelity()
		{
			InitSettings();
			if (_settings == null) { throw new InvalidOperationException(); }

			SetRenderPipelineAsset(_settings.urpAssetHighFidelity);
		}

		[MenuItem("Tools/Switch RenderPipeline/URP-HighFidelity", validate = true)]
		static bool SwitchToURPHighFidelityValidate()
		{
			return !Application.isPlaying;
		}

		[MenuItem("Tools/Switch RenderPipeline/URP-Performant", false, 1)]
		static void SwitchToURPPerformant()
		{
			InitSettings();
			if (_settings == null) { throw new InvalidOperationException(); }

			SetRenderPipelineAsset(_settings.urpAssetPerformant);
		}

		[MenuItem("Tools/Switch RenderPipeline/URP-Performant", validate = true)]
		static bool SwitchToURPHighPerformantValidate()
		{
			return !Application.isPlaying;
		}
#endif

#endif

		static void UpdateMenuCheck()
		{
			var pipelineType = RenderPipelineUtils.DetectPipeline();
#if !NO_BUILTIN
			Menu.SetChecked("Tools/Switch RenderPipeline/Builtin", pipelineType == RenderPipelineUtils.PipelineType.BuiltInPipeline);
#endif
#if USING_HDRP
			Menu.SetChecked("Tools/Switch RenderPipeline/HDRP", pipelineType == RenderPipelineUtils.PipelineType.HDPipeline);
#endif
#if USING_URP
#if !ENABLE_URP_MULTI_QUALITY
			Menu.SetChecked("Tools/Switch RenderPipeline/URP", pipelineType == RenderPipelineUtils.PipelineType.UniversalPipeline);
#else
			InitSettings();
			if (_settings == null) { throw new InvalidOperationException(); }

			var rpAsset = GetCurrentRenderPipelineAsset();
			Menu.SetChecked("Tools/Switch RenderPipeline/URP-Balanced", (pipelineType == RenderPipelineUtils.PipelineType.UniversalPipeline) && (_settings.urpAssetBalanced == rpAsset));
			Menu.SetChecked("Tools/Switch RenderPipeline/URP-HighFidelity", (pipelineType == RenderPipelineUtils.PipelineType.UniversalPipeline) && (_settings.urpAssetHighFidelity == rpAsset));
			Menu.SetChecked("Tools/Switch RenderPipeline/URP-Performant", (pipelineType == RenderPipelineUtils.PipelineType.UniversalPipeline) && (_settings.urpAssetPerformant == rpAsset));
#endif
#endif
		}


#endif
	}
}