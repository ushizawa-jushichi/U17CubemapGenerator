using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     UI 領域におけるポインタードラッグ操作（PointerDown, PointerMove, PointerUp）のイベントを制御するクラス。
    ///     背景やプレビュー領域（dragArea）のドラッグを回転操作として処理します。
    /// </summary>
    public sealed class DragEventHandler : IDisposable
    {
        private readonly VisualElement _dragArea;

        public DragEventHandler(VisualElement dragArea)
        {
            _dragArea = dragArea;

            Assert.IsNotNull(_dragArea, $"{nameof(_dragArea)} is not attached.");

            _dragArea.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _dragArea.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _dragArea.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _dragArea.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            _dragArea.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        }

        public bool IsDragging { get; private set; }

        public Vector2 DragStartPosition { get; private set; }

        public void Dispose()
        {
            _dragArea.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _dragArea.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _dragArea.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _dragArea.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            _dragArea.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            OnPointerDownEvent = null;
            OnPointerMoveEvent = null;
            OnPointerUpEvent = null;
        }

        public event Action<Vector2>? OnPointerDownEvent;
        public event Action<Vector2>? OnPointerMoveEvent;
        public event Action? OnPointerUpEvent;

        private void OnPointerDown(PointerDownEvent evt)
        {
            // 左クリック以外は対象外
            if (evt.button != 0)
            {
                return;
            }

            IsDragging = true;
            DragStartPosition = evt.localPosition;

            _dragArea.CapturePointer(evt.pointerId);
            evt.StopPropagation();

            OnPointerDownEvent?.Invoke(evt.localPosition);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!IsDragging)
            {
                return;
            }

            evt.StopPropagation();

            OnPointerMoveEvent?.Invoke(evt.localPosition);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            _dragArea.ReleasePointer(evt.pointerId);
            evt.StopPropagation();

            EndDrag();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            EndDrag();
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            _dragArea.ReleasePointer(evt.pointerId);
            EndDrag();
        }

        private void EndDrag()
        {
            if (!IsDragging)
            {
                return;
            }

            IsDragging = false;
            OnPointerUpEvent?.Invoke();
        }
    }
}
