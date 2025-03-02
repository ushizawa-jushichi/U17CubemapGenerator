using System;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Rendering;
#if USING_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif
#if USING_URP
using UnityEngine.Rendering.Universal;
#endif

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public partial class U17CubemapGenerator : MonoBehaviour, IU17CubemapGenerator
	{
		RenderPipelineHook? _renderPipelineHook;
		MethodInfo? _renderSingleCamera;
		DateTime _renderStartTime;

		void CleanupRenderCurrentScene()
		{
			_rendererCamera.targetTexture = null;
			_renderPipelineHook?.Dispose();
			_renderPipelineHook = null;
		}

		void VerifyAndSetupRenderCurrentScene(out int cubemapWidth)
		{
			if (_textureWidth <= 0 || _textureWidth >= 4096)
			{
				throw new InvalidOperationException($"Invalid texture size:{_textureWidth}");
			}
			cubemapWidth = _textureWidth;
		}

		void RenderCurrentScene()
		{
			_renderStartTime = DateTime.Now;

			/*
			 * #if UNITY_2023_1_OR_NEWER && USING_HDRP
						if (_pipelineType == RenderPipelineUtility.PipelineType.HDPipeline)
						{
							//_rendererCamera.targetTexture = _cubemapRT; // update rendererCamera
							//_renderPipelineFook = new RenderPipelineFook(
							//	onBeginFrameRendering: (context, cameras) => OnBeginFrameRendering(context, cameras)
							//);
							_onUpdate -= OnRenderCamera;
							_onUpdate += OnRenderCamera;
						}
						else
			#endif
			*/
#if USING_URP
			if (_pipelineType == RenderPipelineUtility.PipelineType.UniversalPipeline)
			{
#if UNITY_2023_1_OR_NEWER
				_onUpdate -= OnRenderCamera;
				_onUpdate += OnRenderCamera;
#else
				_rendererCamera.targetTexture = _cubemapRT; // update rendererCamera trigger
				_renderPipelineHook = new RenderPipelineHook(
					onBeginFrameRendering: OnBeginFrameRendering
				);
#endif
			}
			else
#endif
			{
				_rendererCamera.RenderToCubemap(_cubemapRT);
				var format = _rendererCamera.allowHDR ? UnityEngine.Experimental.Rendering.DefaultFormat.HDR : UnityEngine.Experimental.Rendering.DefaultFormat.LDR;
				var flags = UnityEngine.Experimental.Rendering.TextureCreationFlags.None;
				var cubemap = new Cubemap(_textureWidth, format, flags);
				_rendererCamera.RenderToCubemap(cubemap);
				LoadCubemapFacesFromCubemap(cubemap);
				DestroyImmediate(cubemap);
			}
		}

		void OnRenderCamera()
		{
			try
			{
				var dummy = new ScriptableRenderContext();
				RenderToCubemap(dummy, _rendererCamera);
			}
			finally
			{
				_onUpdate -= OnRenderCamera;
				_isDone = true;
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
				if (_rendererCamera == cameras[i])
				{
					RenderToCubemap(context, cameras[i]);
					_isDone = true;
					break;
				}

				var elapsed = DateTime.Now - _renderStartTime;
				if (elapsed > TimeSpan.FromMilliseconds(5000f))
				{
					_isDone = true;
					throw new TimeoutException("Timeout");
				}
			}
		}

		void RenderToCubemap(ScriptableRenderContext context, Camera camera)
		{
			if (_materialBlitter == null) { throw new InvalidOperationException($"{nameof(_materialBlitter)} null."); }

			int texWidth = _textureWidth;
			var desc = GetRenderTextureDescriptorForOutputTemporary(texWidth, texWidth, true);
			var tempRT = RenderTexture.GetTemporary(desc);
			var tempRT2 = RenderTexture.GetTemporary(desc);
			DisposeCachedFaces();
			camera.fieldOfView = 90f;
			camera.orthographic = false;
			var lastInverseCulling = GL.invertCulling;

			for (int i = 0; i < 6; i++)
			{
				var face = (CubemapFace)i;

				GL.invertCulling = true;
				RenderFace(context, camera, face, tempRT);
				GL.invertCulling = lastInverseCulling;

				_materialBlitter.SetVector("_MainTex_ST_", new Vector4(1f, 1f, 0f, 0f));
				Graphics.SetRenderTarget(_cubemapRT, 0, face);
				Graphics.Blit(tempRT, _materialBlitter, pass: 0);

				_materialBlitter.SetVector("_MainTex_ST_", new Vector4(1f, -1f, 0f, 1f));
				Graphics.SetRenderTarget(tempRT2, 0);
				Graphics.Blit(tempRT, _materialBlitter, pass: 0);

				RenderTexture prev = RenderTexture.active;
				RenderTexture.active = tempRT2;
				_cachedFaces[i] = new Texture2D(texWidth, texWidth);
				_cachedFaces[i]!.ReadPixels(new Rect(0, 0, texWidth, texWidth), 0, 0);
				_cachedFaces[i]!.Apply();
				RenderTexture.active = prev;
			}

			camera.targetTexture = null; 
			RenderTexture.ReleaseTemporary(tempRT2);
			RenderTexture.ReleaseTemporary(tempRT);
		}

		void RenderFace(ScriptableRenderContext context, Camera camera, CubemapFace face, RenderTexture rt)
		{
			Quaternion rot;
			switch (face)
			{
				case CubemapFace.PositiveX: rot = Quaternion.Euler(0f, 90f, 0f); break;
				case CubemapFace.NegativeX: rot = Quaternion.Euler(0f, -90f, 0f); break;
				case CubemapFace.PositiveY: rot = Quaternion.Euler(-90f, 0f, 0f); break;
				case CubemapFace.NegativeY: rot = Quaternion.Euler(90f, 0f, 0f); break;
				case CubemapFace.PositiveZ: rot = Quaternion.Euler(0f, 0f, 0f); break;
				case CubemapFace.NegativeZ: rot = Quaternion.Euler(0f, 180f, 0f); break;
				default: throw new ArgumentException("face:" + face);
			}

			camera.transform.rotation = _rotationBase * rot;
			camera.targetTexture = rt;
			camera.ResetProjectionMatrix();
			camera.projectionMatrix = new Matrix4x4(
				new Vector4(1f, 0f, 0f, 0f),
				new Vector4(0f,-1f, 0f, 0f),
				new Vector4(0f, 0f, 1f, 0f),
				new Vector4(0f, 0f, 0f, 1f)) * camera.projectionMatrix;
#if USING_URP 
			if (_pipelineType == RenderPipelineUtility.PipelineType.UniversalPipeline)
			{
#if UNITY_2023_3_OR_NEWER
				// 2022_1_OR_NEWERでは描画がおかしい
				var request = new UniversalRenderPipeline.SingleCameraRequest();
				if (RenderPipeline.SupportsRenderRequest(camera, request))
				{
					request.destination = rt;
					request.face = face;
					RenderPipeline.SubmitRenderRequest(camera, request);
				}
				else
				{
					Debug.LogWarning($"UniversalPipeline: RenderRequest failed");
				}
#else
				UniversalRenderPipeline.RenderSingleCamera(context, camera);
#endif
				// RenderSingleCamera is warned as obsolete, but the alternative is an error.(2023/05/22)
				// https://forum.unity.com/threads/rendersinglecamera-is-obsolete-but-the-suggested-solution-has-error.1354835/
			}
			else
#endif
#if USING_HDRP
			if (_pipelineType == RenderPipelineUtility.PipelineType.HDPipeline)
			{
#if UNITY_2023_3_OR_NEWER
				var request = new HDRenderPipeline.StandardRequest();
				if (HDRenderPipeline.SupportsRenderRequest(camera, request))
				{
					request.destination = rt;
					request.face = face;
					HDRenderPipeline.SubmitRenderRequest(camera, request);
				}
				else
				{
					Debug.LogWarning($"HDPipeline: RenderRequest failed");
				}
#else
				camera.Render();
#endif
			}
#endif

			camera.targetTexture = null;
		}
	}
}
