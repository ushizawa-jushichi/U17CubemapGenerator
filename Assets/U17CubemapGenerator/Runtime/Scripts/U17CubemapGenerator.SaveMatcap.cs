using System;
using System.Collections;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
#if USING_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif
#if USING_URP
using UnityEngine.Rendering.Universal;
#endif

#nullable enable
#pragma warning disable 618 // obsolete
#pragma warning disable 414 // The field '' is assigned but its value is never used
#pragma warning disable 162 // Unreachable code detected

namespace Ushino17
{
	public partial class U17CubemapGenerator : MonoBehaviour, IU17CubemapGenerator
	{
		[SerializeField] ComputeShader _computeShader = null!;

#if UNITY_EDITOR

		class SaveAsMatcap : CubemapSaveBase
		{
			RenderTexture? _tempRT;
			RenderPipelineHook? _renderPipelineHook;
			bool _isDone;

			public SaveAsMatcap(U17CubemapGenerator generator)
				: base(generator)
			{
				this.generator.UpdatePreviewObjectVisibility(InternalPreviewObjectMode.Matcap);
			}

			public override void Dispose()
			{
				if (_tempRT != null)
				{
					RenderTexture.ReleaseTemporary(_tempRT);
					_tempRT = null;
				}
				_renderPipelineHook?.Dispose();
				_renderPipelineHook = null;
				generator._rendererCamera.gameObject.SetActive(false);
				generator.UpdatePreviewObjectVisibility(InternalPreviewObjectMode.Default);
				base.Dispose();
			}

			public override IEnumerator SaveAsPNGCoroutine(string assetPath)
			{
				const int LayerNo_CubemapGeneratorMatcap = 31;

				var targetSourceCamera = generator.GetTargetCamera();
				generator._rendererCamera.CopyFrom(targetSourceCamera);
				generator._rendererCamera.gameObject.SetActive(true);

				const float Adjust = 1.010f;
				float distance = 1f / Mathf.Atan(Adjust * targetSourceCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

				var position = targetSourceCamera.transform.position + -targetSourceCamera.transform.forward * distance;
				generator._rendererCamera.transform.position = position;
				generator._rendererCamera.orthographic = false;
				generator._rendererCamera.usePhysicalProperties = false;
				generator._rendererCamera.clearFlags = CameraClearFlags.SolidColor;
				generator._rendererCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
				generator._rendererCamera.cullingMask = (1 << LayerNo_CubemapGeneratorMatcap);
				generator._previewSphereMatcap.transform.position = targetSourceCamera.transform.position;
				generator._previewSphereMatcap.layer = LayerNo_CubemapGeneratorMatcap;

				var desc = generator.GetRenderTextureDescriptorForOutputTemporary(generator._textureWidth, generator._textureWidth, true);
				_tempRT = RenderTexture.GetTemporary(desc);

				generator._renderStartTime = DateTime.Now;

#if USING_HDRP
				if (generator._exposureOverride && (generator._pipelineType == RenderPipelineUtils.PipelineType.HDPipeline))
				{
					generator.StartExposureOverride(generator._fixedExposure, generator._compensation);
				}
#endif

				if (generator._pipelineType == RenderPipelineUtils.PipelineType.BuiltInPipeline
#if USING_HDRP 
					|| generator._pipelineType == RenderPipelineUtils.PipelineType.HDPipeline
#endif
					)
				{
					generator._onUpdate -= OnRenderCamera;
					generator._onUpdate += OnRenderCamera;

				}
				else
				{
					generator._rendererCamera.targetTexture = _tempRT; // update rendererCamera trigger
					_renderPipelineHook = new RenderPipelineHook(
						onBeginFrameRendering: (context, cameras) => OnBeginFrameRendering(context, cameras)
					);
				}
				yield return new WaitUntil(() => _isDone);

#if USING_HDRP
				generator.CleanupExposureOverride();
#endif

				if (generator._fillMatcapOutsideByEdgeColor)
				{
					RunComputeMatcapFillOutsideByEdgeColor(desc);
				}

				RenderTexture previous = RenderTexture.active;
				RenderTexture.active = _tempRT;

				var tempTex = generator.CreateTexture2DForOutputTemporary(_tempRT.width, _tempRT.height);
				tempTex.ReadPixels(new Rect(0, 0, _tempRT.width, _tempRT.height), 0, 0);
				tempTex.Apply();

				var bytes = tempTex.EncodeToPNG();   
				File.WriteAllBytes(assetPath, bytes);

				RenderTexture.active = previous;

				RenderTexture.ReleaseTemporary(_tempRT);
				_tempRT = null;

				_renderPipelineHook?.Dispose();
				_renderPipelineHook = null;

				AssetDatabase.ImportAsset(assetPath);
				U17CubemapGenerator.SetOutputSpecification(assetPath,
					TextureImporterShape.Texture2D,
					generator._isOutputGenerateMipmap,
					generator._isOutputSRGB);
				AssetDatabase.Refresh();

				yield break;
			}

			void OnRenderCamera()
			{
				try
				{
					var dummy = new ScriptableRenderContext();
					RenderMatcap(dummy, generator._rendererCamera);
				}
				finally
				{
					_isDone = true;
					generator._rendererCamera.gameObject.SetActive(false);
					generator._onUpdate -= OnRenderCamera;
				}
			}

			void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
			{
				if (_isDone)
				{
					return;
				}

				for (int i = 0; i < cameras.Length; i++)
				{
					if (generator._rendererCamera == cameras[i])
					{
						RenderMatcap(context, cameras[i]);
						_isDone = true;
						break;
					}

					var elapsed = DateTime.Now - generator._renderStartTime;
					if (elapsed > TimeSpan.FromMilliseconds(5000f))
					{
						_isDone = true;
						throw new TimeoutException("Timeout");
					}
				}
			}

			void RenderMatcap(ScriptableRenderContext context, Camera camera)
			{
				camera.targetTexture = _tempRT;

#if USING_HDRP
				if (generator._pipelineType == RenderPipelineUtils.PipelineType.HDPipeline)
				{
				}
				else
#endif
#if USING_URP
				if (generator._pipelineType == RenderPipelineUtils.PipelineType.UniversalPipeline)
				{
					UniversalRenderPipeline.RenderSingleCamera(context, camera);
				}
				else
#endif
				{
					camera.Render();
				}

				camera.targetTexture = null;
			}

			void RunComputeMatcapFillOutsideByEdgeColor(RenderTextureDescriptor desc)
			{
				if (_tempRT == null) throw new InvalidOperationException($"{nameof(_tempRT)} null");

				var tempRT2 = RenderTexture.GetTemporary(desc);
				Graphics.CopyTexture(_tempRT, tempRT2);

				int width = _tempRT.width;
				int kernelIndex = generator._computeShader.FindKernel("MatcapFillOutsideByEdgeColor");
				generator._computeShader.SetTexture(kernelIndex, "source", tempRT2);
				generator._computeShader.SetTexture(kernelIndex, "result", _tempRT);
				generator._computeShader.SetInt("width", width);
				generator._computeShader.GetKernelThreadGroupSizes(kernelIndex, out var threadSizeX, out var threadSizeY, out var threadSizeZ);
				generator._computeShader.Dispatch(kernelIndex, width / (int)threadSizeX, width / (int)threadSizeY, (int)threadSizeZ);

				RenderTexture.ReleaseTemporary(tempRT2);
			}
		}
#endif
	}
}