using System.Collections;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

#nullable enable
#pragma warning disable 1998

namespace Uchuhikoshi.U17CubemapGenerator
{
	public partial class U17CubemapGenerator : MonoBehaviour, IU17CubemapGenerator
	{
#if UNITY_EDITOR
		class SaveAs6Sided : CubemapSaveBase
		{
			public SaveAs6Sided(U17CubemapGenerator generator) : base(generator) {}

			public override IEnumerator SaveAsPNGCoroutine(string assetPath)
			{
				var suffixes = new string[] { "_xplus", "_xminus", "_yplus", "_yminus", "_zplus", "_zminus" };
				string assetExt = Path.GetExtension(assetPath);
				for (int i = 0; i < 6; i++)
				{
					string assetPathTmp = Path.ChangeExtension(assetPath, null) + $"{suffixes[i]}{assetExt}";
					var bytes = generator._cachedFaces[i].EncodeToPNG();
					File.WriteAllBytes(assetPathTmp, bytes);
					AssetDatabase.ImportAsset(assetPathTmp);
					U17CubemapGenerator.SetOutputSpecification(assetPathTmp,
						TextureImporterShape.Texture2D,
						generator._isOutputGenerateMipmap,
						generator._isOutputSRGB);
					AssetDatabase.Refresh();
				}
				AssetDatabase.Refresh();
				yield break;
			}
		}
#endif
	}
}
