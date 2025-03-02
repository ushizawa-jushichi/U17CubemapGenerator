using UnityEditor;
using UnityEngine;

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public interface IU17CubemapGeneratorWindow
	{
		EditorWindow Window { get; }
		Rect MainViewRect { get; }
	}
}