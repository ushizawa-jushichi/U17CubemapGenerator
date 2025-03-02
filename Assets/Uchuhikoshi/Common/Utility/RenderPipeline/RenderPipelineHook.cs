using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#nullable enable

namespace Uchuhikoshi
{
	public class RenderPipelineHook : IDisposable
	{
#if UNITY_2023_3_OR_NEWER
		event Action<ScriptableRenderContext, List<Camera>>? _onBeginContextRendering;
		event Action<ScriptableRenderContext, List<Camera>>? _onEndContextRendering;
#else
		event Action<ScriptableRenderContext, Camera[]>? _onBeginFrameRendering;
		event Action<ScriptableRenderContext, Camera[]>? _onEndFrameRendering;
#endif
		event Action<ScriptableRenderContext, Camera>? _onBeginCameraRendering;
		event Action<ScriptableRenderContext, Camera>? _onEndCameraRendering;

		public RenderPipelineHook(
#if UNITY_2023_3_OR_NEWER
			Action<ScriptableRenderContext, List<Camera>>? onBeginContextRendering = null,
			Action<ScriptableRenderContext, List<Camera>>? onEndContextRendering = null,
#else
			Action<ScriptableRenderContext, Camera[]>? onBeginFrameRendering = null,
			Action<ScriptableRenderContext, Camera[]>? onEndFrameRendering = null,
#endif
			Action<ScriptableRenderContext, Camera>? onBeginCameraRendering = null,
			Action<ScriptableRenderContext, Camera>? onEndCameraRendering = null
			)
		{
#if UNITY_2023_3_OR_NEWER
			_onBeginContextRendering = onBeginContextRendering;
			_onEndContextRendering = onEndContextRendering;
#else
			_onBeginFrameRendering = onBeginFrameRendering;
			_onEndFrameRendering = onEndFrameRendering;
#endif
			_onBeginCameraRendering = onBeginCameraRendering;
			_onEndCameraRendering = onEndCameraRendering;

#if UNITY_2023_3_OR_NEWER
			if (_onBeginContextRendering != null)
			{
				RenderPipelineManager.beginContextRendering -= _onBeginContextRendering;
				RenderPipelineManager.beginContextRendering += _onBeginContextRendering;
			}
			if (_onEndContextRendering != null)
			{
				RenderPipelineManager.endContextRendering -= _onEndContextRendering;
				RenderPipelineManager.endContextRendering += _onEndContextRendering;
			}
#else
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
#endif
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
#if UNITY_2023_3_OR_NEWER
			if (_onBeginContextRendering != null)
			{
				RenderPipelineManager.beginContextRendering -= _onBeginContextRendering;
				_onBeginContextRendering = null;
			}
			if (_onEndContextRendering != null)
			{
				RenderPipelineManager.endContextRendering -= _onEndContextRendering;
				_onEndContextRendering = null;
			}
#else
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
#endif
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
