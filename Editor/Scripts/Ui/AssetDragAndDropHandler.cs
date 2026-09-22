using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     ウィンドウへのテクスチャやキューブマップのドラッグ＆ドロップ操作を受け付け、状態に反映するハンドラー。
    /// </summary>
    public sealed class AssetDragAndDropHandler : IDisposable
    {
        private readonly EditorState _editorState;
        private readonly Action _onAssetDropped;
        private readonly Settings _settings;
        private readonly VisualElement _targetElement;

        public AssetDragAndDropHandler(
            VisualElement targetElement,
            EditorState editorState,
            Settings settings,
            Action onAssetDropped)
        {
            _targetElement = targetElement;
            _editorState = editorState;
            _settings = settings;
            _onAssetDropped = onAssetDropped;

            _targetElement.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            _targetElement.RegisterCallback<DragPerformEvent>(OnDragPerform);
        }

        public void Dispose()
        {
            if (_targetElement != null)
            {
                _targetElement.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
                _targetElement.UnregisterCallback<DragPerformEvent>(OnDragPerform);
            }
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (HasAcceptableAssets(DragAndDrop.objectReferences))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            }
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (!HasAcceptableAssets(DragAndDrop.objectReferences))
            {
                return;
            }

            DragAndDrop.AcceptDrag();
            var stateChanged = false;

            if (DragAndDrop.objectReferences.Length > 0)
            {
                Undo.RecordObject(_editorState, "Drag and Drop Asset");
            }

            var textureCount = 0;
            for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
            {
                if (DragAndDrop.objectReferences[i] is Texture2D)
                {
                    textureCount++;
                }
            }

            var isSingleTextureDrop = textureCount == 1;
            var unassignedNames = new List<string>();

            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is Cubemap cubemap)
                {
                    _editorState.SetDroppedCubemap(cubemap);
                    stateChanged = true;
                }
                else if (obj is Texture2D texture)
                {
                    var assigned = _editorState.SetDroppedSixSidedTexture(texture, _settings, isSingleTextureDrop);
                    if (assigned)
                    {
                        stateChanged = true;
                    }
                    else
                    {
                        unassignedNames.Add(texture.name);
                    }
                }
            }

            if (unassignedNames.Count > 0)
            {
                Debug.LogWarning(
                    $"[CubemapGenerator] Could not identify cubemap face for {unassignedNames.Count} texture(s):\n" +
                    string.Join(", ", unassignedNames) +
                    "\nYou can configure supported suffixes in U17CubemapGeneratorSettings asset.");
            }

            if (stateChanged)
            {
                _onAssetDropped?.Invoke();
            }

            evt.StopPropagation();
        }

        private static bool HasAcceptableAssets(Object[] objects)
        {
            if (objects == null || objects.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i] is Texture2D or Cubemap)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
