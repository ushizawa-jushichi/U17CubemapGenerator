using System;
using UnityEditor;

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public static class AssetPath
	{
		static string? _assetPath = null;

		public static string U17CubemapGenerator
		{
			get
			{
				if (_assetPath == null)
				{
					string[] guids = AssetDatabase.FindAssets("U17CubemapGenerator.Editor", null);
					if (guids.Length > 0) 
					{
						var tmp = AssetDatabase.GUIDToAssetPath(guids[0]);
						tmp = tmp.Replace("Editor/U17CubemapGenerator.Editor.asmdef", string.Empty);
						_assetPath = tmp;
					}
				}
				if (_assetPath == null)
				{
					throw new InvalidOperationException("Asset path not found s");
				}
				return _assetPath;
			}
		}
	}
}