using UnityEngine;
using UnityEditor;

#nullable enable

namespace Uchuhikoshi
{
	public sealed class EditorFpsCounter
	{
		int _counter = 0;
		float _elapsed = 0f;
		int _fps = 0;
		public int Fps => _fps;

		public void Reset()
		{
			_counter = 0;
			_elapsed = 0f;
			_fps = 0;
		}

		public void OnUpdate(float deltaTime)
		{
			_counter++;
			_elapsed += deltaTime;
			if (_elapsed >= 1.0f)
			{
				_fps = _counter / (int)Mathf.Floor(_elapsed);
				_counter = 0;
				_elapsed = _elapsed - Mathf.FloorToInt(_elapsed);
			}
		}
	}
}
