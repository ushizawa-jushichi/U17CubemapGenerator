using UnityEngine;
#if USING_URP
using UnityEngine.Rendering.Universal;
#endif

#nullable enable

namespace Uchuhikoshi
{
	public static class CameraUtility
	{
		public static void CopyCameraParameterAndTransform(
			Camera destination, Camera source,
			bool withLocalTransform = false,
			bool withTransform = false,
			bool withAdditionalCameraData = true
			)
		{
			destination.CopyFrom(source);

			if (withLocalTransform)
			{
#if UNITY_2022_1_OR_NEWER
				source.transform.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
				destination.transform.SetLocalPositionAndRotation(localPosition, localRotation);
#else
				destination.transform.localPosition = source.transform.localPosition;
				destination.transform.localRotation = source.transform.localRotation;
#endif
			}
			else if (withTransform)
			{
#if UNITY_2022_1_OR_NEWER
				source.transform.GetPositionAndRotation(out var position, out var rotation);
#else
				var position = source.transform.localPosition;
				var rotation = source.transform.localRotation;
#endif
				destination.transform.SetPositionAndRotation(position, rotation);
			}

			if (withAdditionalCameraData)
			{
#if USING_URP
				var srcData = source.GetComponent<UniversalAdditionalCameraData>();
				var destData = destination.GetComponent<UniversalAdditionalCameraData>();
				if (srcData != null && destData != null)
				{
					CopyURPCameraData(destData, srcData);
				}
#endif
			}
		}

#if USING_URP
		public static void CopyURPCameraData(UniversalAdditionalCameraData destination, UniversalAdditionalCameraData source)
		{
			destination.renderShadows = source.renderShadows;
			destination.requiresDepthOption = source.requiresDepthOption;
			destination.requiresColorOption = source.requiresColorOption;
			destination.renderType = source.renderType;
			destination.requiresDepthTexture = source.requiresDepthTexture;
			destination.requiresColorTexture = source.requiresColorTexture;
			destination.volumeLayerMask = source.volumeLayerMask;
			destination.volumeTrigger = source.volumeTrigger;
			destination.volumeStack = source.volumeStack;
			destination.renderPostProcessing = source.renderPostProcessing;
			destination.antialiasing = source.antialiasing;
			destination.antialiasingQuality = source.antialiasingQuality;
#if UNITY_2022_1_OR_NEWER
			destination.resetHistory = source.resetHistory;
#endif
			destination.stopNaN = source.stopNaN;
			destination.dithering = source.dithering;
			destination.allowXRRendering = source.allowXRRendering;
#if UNITY_2022_1_OR_NEWER
			destination.useScreenCoordOverride = source.useScreenCoordOverride;
			destination.screenSizeOverride = source.screenSizeOverride;
			destination.screenCoordScaleBias = source.screenCoordScaleBias;
			destination.allowHDROutput = source.allowHDROutput;
#endif
		}

		public static void CopyURPCameraData(Camera destination, Camera source)
		{
			CopyURPCameraData(destination.GetComponent<UniversalAdditionalCameraData>(), source.GetComponent<UniversalAdditionalCameraData>());
		}
#endif
	}
}
