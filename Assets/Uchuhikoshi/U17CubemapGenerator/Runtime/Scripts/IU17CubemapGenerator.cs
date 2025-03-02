using System;
using UnityEngine;

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public interface IU17CubemapGenerator
	{
		bool IsAvailableInspector6Sided { get; }
		bool IsProcessing { get; }
		bool IsPipelineChanging { get; }
#if UNITY_EDITOR
		GameObject PreviewSphere { get; }
		GameObject PreviewCube { get; }
		bool IsSourceHDR { get; }
		Action? OnPipelineChanged { get; set; }
#endif

		void SetPreviewObject(U17CubemapGenerator.PreviewObject previewObject);
		void SetUsingCameraAngles(bool usingCameraAngles);
		void SetSpecificCamera(Camera? specificCamera);
		void SetCubemapWidth(int textureWidth);
		void StartRender(U17CubemapGenerator.InputSource inputSource, Action? onCompleted = null);
		void ResetPreviewCubeRotation();
		void ClearCubemap();
		void ClearDragging();
		void SetExposureOverride(bool exposureOverride, float fixedExposure, float compensation);

#if UNITY_EDITOR
		void SetCubemapOutputEquirectangularRotation(float angleEulerY);
		void SetCubemapOutputLayout(U17CubemapGenerator.OutputLayout cubemapOutputLayout);
		void SaveAsset(string path, Action<string>? onCompleted = null);
		void SetOutputDesirableHDR(bool value);
		void SetOutputDesirableSRGB(bool value);
		void SetOutputDesirableCubemap(bool value);
		void SetOutputDesirableGenerateMipmap(bool value);
		void SetFillMatcapOutsideByEdgeColor(bool value);
		void SetEditorDragControl(bool value);
		void SetEditorMousePosition(Vector2 position);
		void SetEditorMouseDown();
		void SetSourceCubemap(Cubemap cubemap);
		void SetSourceTexture(CubemapFace face, Texture2D texture);
#endif
	}
}
