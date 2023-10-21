using UnityEngine;

#nullable enable

namespace Ushino17
{
	public partial class U17CubemapGenerator : MonoBehaviour, IU17CubemapGenerator
	{
		[SerializeField] float _dragSpeedScale = 30f;
		[SerializeField] bool _editorDragControl;

		Vector2 _screenPositionCache;
		bool _isPressed;
		bool _isDragging;
		Quaternion _previewRotation = Quaternion.identity;

#if UNITY_EDITOR
		Vector2 _editorMousePosition;
		bool _editorMousePressed;

		public void SetEditorDragControl(bool value)
		{
			_editorDragControl = value;
		}

		public void SetEditorMousePosition(Vector2 position)
		{
			_editorMousePosition = position;
		}
		public void SetEditorMouseDown()
		{
			_editorMousePressed = true;
		}
		public void SetEditorMouseUp()
		{
			_editorMousePressed = false;
		}

#endif

		void OnUpdateDragRotate()
		{
			bool isPreviousPressed = _isPressed;
			Vector2 screenPosition = _screenPositionCache;

#if UNITY_EDITOR
			if (_editorDragControl)
			{
				_isPressed = _editorMousePressed;
				if (_isPressed)
				{
					screenPosition = _editorMousePosition;
				}
			}
#endif

			if (_isPressed && isPreviousPressed)
			{
				_isDragging = true;
			}
			else
			{
				_isDragging = false;
			}

			if (_isDragging)
			{
				var delta = screenPosition - _screenPositionCache;
				if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
				{
					delta.x = 0f;
				}
				else
				{
					delta.y = 0f;
				}
				Vector3 angleDelta = new Vector3(-delta.y, -delta.x, 0f) * _dragSpeedScale * Time.deltaTime;
				Quaternion r = _previewRotation;
				r = Quaternion.AngleAxis(angleDelta.x, Vector3.right) * r;
				r = Quaternion.AngleAxis(angleDelta.y, Vector3.up) * r;
				_previewRotation  = r;
				UpdatePreviewObjectRotation();
			}

			_screenPositionCache = screenPosition;
		}

		void UpdatePreviewObjectRotation()
		{
			_previewCube.transform.rotation = _previewRotation;
			_previewRotationMatrix = Matrix4x4.Rotate(_previewRotation);
			UpdatePreviewMeshMatrix();
		}

		public void ResetPreviewCubeRotation()
		{
			_previewRotation = Quaternion.identity;
			UpdatePreviewObjectRotation();
		}

		public void ClearDragging()
		{
			_isDragging = false;
		}
	}
}
