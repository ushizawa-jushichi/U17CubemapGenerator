using System.Collections;
using UnityEditor;
using UnityEngine;

// https://gist.github.com/benblo/10732554

namespace Uchuhikoshi
{
	public class TinyEditorCoroutine
	{
		public static TinyEditorCoroutine Start( IEnumerator _routine )
		{
			TinyEditorCoroutine coroutine = new TinyEditorCoroutine(_routine);
			coroutine.Start();
			return coroutine;
		}

		readonly IEnumerator routine;
		TinyEditorCoroutine( IEnumerator _routine )
		{
			routine = _routine;
		}

		void Start()
		{
			//Debug.Log("start");
			EditorApplication.update += Update;
		}

		public void Stop()
		{
			//Debug.Log("stop");
			EditorApplication.update -= Update;
		}

		void Update()
		{
			/* NOTE: no need to try/catch MoveNext,
			 * if an IEnumerator throws its next iteration returns false.
			 * Also, Unity probably catches when calling EditorApplication.update.
			 */
			//Debug.Log("update");
			if (!routine.MoveNext())
			{
				Stop();
			}
		}
	}
}
