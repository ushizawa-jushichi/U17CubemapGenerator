using System;
using System.Collections;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public partial class U17CubemapGenerator : MonoBehaviour, IU17CubemapGenerator
	{
#if UNITY_EDITOR
		class SaveAsCubemap : CubemapSaveBase
		{
			public SaveAsCubemap(U17CubemapGenerator generator) : base(generator) { }

			public override IEnumerator SaveAsPNGCoroutine(string assetPath)
			{
				int texWidth = generator._cachedFaces[0]!.width;
				var blockSize = new Vector2Int(texWidth, texWidth);
				var tmpSize = blockSize;
				var destPos = new Vector2Int[6];

				switch (generator._outputLayout)
				{
					case OutputLayout.CrossHorizontal:
						tmpSize.x *= 4;
						tmpSize.y *= 3;
						destPos[0] = new Vector2Int(2, 1); // +X
						destPos[1] = new Vector2Int(0, 1); // -X
						destPos[2] = new Vector2Int(1, 2); // +Y
						destPos[3] = new Vector2Int(1, 0); // -Y
						destPos[4] = new Vector2Int(1, 1); // +Z
						destPos[5] = new Vector2Int(3, 1); // -Z
						break;
					case OutputLayout.CrossVertical:
						tmpSize.x *= 3;
						tmpSize.y *= 4;
						destPos[0] = new Vector2Int(1, 2); // +X
						destPos[1] = new Vector2Int(1, 0); // -X
						destPos[2] = new Vector2Int(1, 3); // +Y
						destPos[3] = new Vector2Int(1, 1); // -Y
						destPos[4] = new Vector2Int(0, 2); // +Z
						destPos[5] = new Vector2Int(2, 2); // -Z
						break;
					case OutputLayout.StraitHorizontal:
						tmpSize.x *= 6;
						for (int i = 0; i < 6; i++)
						{
							destPos[i] = new Vector2Int(i, 0);
						}
						break;
					case OutputLayout.StraitVertical:
						tmpSize.y *= 6;
						for (int i = 0; i < 6; i++)
						{
							destPos[i] = new Vector2Int(0, 5 - i);
						}
						break;
					default:
						throw new InvalidOperationException("Unsupported OutputLayout type");
				}

				Texture2D tempTex = generator.CreateTexture2DForOutputTemporary(tmpSize.x, tmpSize.y);
				TextureUtility.FillTexture2D(tempTex, Color.black);

				for (int i = 0; i < 6; i++)
				{
					tempTex.SetPixels(destPos[i].x * blockSize.x, destPos[i].y * blockSize.y, blockSize.x, blockSize.y, generator._cachedFaces[i]!.GetPixels(), 0);
				}
				tempTex.Apply();

				var bytes = tempTex.EncodeToPNG();
				File.WriteAllBytes(assetPath, bytes);
				AssetDatabase.ImportAsset(assetPath);
				U17CubemapGenerator.SetOutputSpecification(assetPath,
					(generator._isOutputCubemap ? TextureImporterShape.TextureCube : TextureImporterShape.Texture2D),
					generator._isOutputGenerateMipmap,
					generator._isOutputSRGB);
				AssetDatabase.Refresh();

				yield break;
			}

		}

#endif
	}
}
