using System;
using UnityEngine;
using UnityEngine.Rendering;

#nullable enable

namespace Uchuhikoshi
{
	public class RenderPipelineHook : IDisposable
	{
		Action<ScriptableRenderContext, Camera[]>? _onBeginFrameRendering;
		Action<ScriptableRenderContext, Camera[]>? _onEndFrameRendering;
		Action<ScriptableRenderContext, Camera>? _onBeginCameraRendering;
		Action<ScriptableRenderContext, Camera>? _onEndCameraRendering;

		public RenderPipelineHook(
			Action<ScriptableRenderContext, Camera[]>? onBeginFrameRendering = null,
			Action<ScriptableRenderContext, Camera[]>? onEndFrameRendering = null,
			Action<ScriptableRenderContext, Camera>? onBeginCameraRendering = null,
			Action<ScriptableRenderContext, Camera>? onEndCameraRendering = null
			)
		{
			_onBeginFrameRendering = onBeginFrameRendering;
			_onEndFrameRendering = onEndFrameRendering;
			_onBeginCameraRendering = onBeginCameraRendering;
			_onEndCameraRendering = onEndCameraRendering;

			if (_onBeginFrameRendering != null)
			{
				RenderPipelineManager.beginFrameRendering -= _onBeginFrameRendering;
				RenderPipelineManager.beginFrameRendering += _onBeginFrameRendering;
			}
			if (_onEndFrameRendering != null)
			{
				RenderPipelineManager.endFrameRendering -= _onEndFrameRendering;
				RenderPipelineManager.endFrameRendering += _onEndFrameRendering;
			}
			if (_onBeginCameraRendering != null)
			{
				RenderPipelineManager.beginCameraRendering -= _onBeginCameraRendering;
				RenderPipelineManager.beginCameraRendering += _onBeginCameraRendering;
			}
			if (_onEndCameraRendering != null)
			{
				RenderPipelineManager.endCameraRendering -= _onEndCameraRendering;
				RenderPipelineManager.endCameraRendering += _onEndCameraRendering;
			}
		}

		public void Dispose()
		{
			if (_onBeginFrameRendering != null)
			{
				RenderPipelineManager.beginFrameRendering -= _onBeginFrameRendering;
				_onBeginFrameRendering = null;
			}
			if (_onEndFrameRendering != null)
			{
				RenderPipelineManager.endFrameRendering -= _onEndFrameRendering;
				_onEndFrameRendering = null;
			}
			if (_onBeginCameraRendering != null)
			{
				RenderPipelineManager.beginCameraRendering -= _onBeginCameraRendering;
				_onBeginCameraRendering = null;
			}
			if (_onEndCameraRendering != null)
			{
				RenderPipelineManager.endCameraRendering -= _onEndCameraRendering;
				_onEndCameraRendering = null;
			}
		}
	}
}
