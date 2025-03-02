using UnityEditor;

#nullable enable

namespace Uchuhikoshi
{
	public sealed class EditorDeltaTime
	{
		float _editorDeltaTimef = 0f;
		double _editorDeltaTime = 0f;
		double _lastTimeSinceStartup = 0f;

		public float editorDeltaTimef => _editorDeltaTimef;
		public double editorDeltaTime => _editorDeltaTime;

		public void OnUpdate()
		{
			if (_lastTimeSinceStartup == 0f)
			{
				_lastTimeSinceStartup = EditorApplication.timeSinceStartup;
			}
			_editorDeltaTime = EditorApplication.timeSinceStartup - _lastTimeSinceStartup;
			_editorDeltaTimef = (float)_editorDeltaTime;
			_lastTimeSinceStartup = EditorApplication.timeSinceStartup;
		}
	}
}
