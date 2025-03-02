using UnityEngine;

#nullable enable

namespace Uchuhikoshi
{
	public static class MeshRenderUtility
	{
		public static Bounds CalculateMaxBounds(GameObject go)
		{
			var maxBounds = new Bounds(Vector3.zero, Vector3.zero);
			var meshFilters = go.GetComponentsInChildren<MeshFilter>(true);
			foreach (var meshFilter in meshFilters)
			{
				maxBounds.Encapsulate(meshFilter.sharedMesh.bounds);
			}

			var skinnedMeshes = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			foreach (var skinnedMeshe in skinnedMeshes)
			{
				maxBounds.Encapsulate(skinnedMeshe.sharedMesh.bounds);
			}
			return maxBounds;
		}

    }
}
