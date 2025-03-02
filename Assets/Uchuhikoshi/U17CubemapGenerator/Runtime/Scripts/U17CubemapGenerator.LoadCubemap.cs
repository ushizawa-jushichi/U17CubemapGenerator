using System;
using UnityEngine;

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public partial class U17CubemapGenerator : MonoBehaviour, IU17CubemapGenerator
	{
		[SerializeField] Cubemap? _texCubemap;

#if UNITY_EDITOR
		public void SetSourceCubemap(Cubemap cubemap)
		{
			if (this.IsProcessing) { throw new InvalidOperationException("Now processing"); }
			_texCubemap = cubemap;
		}
#endif

		void VerifyAndSetupLoadCubemap()
		{
			if (_texCubemap == null) throw new InvalidOperationException($"{nameof(_texCubemap)} null");
		}

		void LoadCubemapFaces()
		{
			if (_texCubemap == null) throw new InvalidOperationException($"{nameof(_texCubemap)} null");
			LoadCubemapFacesFromCubemap(_texCubemap);
		}

		void LoadCubemapFacesFromCubemap(Cubemap cubemap)
		{
			if (cubemap == null) { throw new ArgumentException($"{nameof(cubemap)} null"); }

			int texWidth = cubemap.width;
			var readableCubeMap = new Cubemap(texWidth, cubemap.format, (cubemap.mipmapCount > 1));
			for (int i = 0; i < 6; i++)
			{
				Graphics.CopyTexture(cubemap, i, readableCubeMap, i);
			}

			DisposeCachedFaces();

			_isSourceHDR = cubemap.format.IsHDRFormat();
			var textureFormat = _isSourceHDR ? TextureFormat.RGBAHalf : TextureFormat.RGBA32;
			var renderTextureFormat = _isSourceHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32;
			var desc = new RenderTextureDescriptor(texWidth, texWidth, renderTextureFormat, 32);
#if UNITY_2022_1_OR_NEWER
			desc.sRGB = cubemap.isDataSRGB;
#endif
			RenderTexture? tempRT = RenderTexture.GetTemporary(desc);

			bool isLinear = false;
#if UNITY_2022_1_OR_NEWER
			isLinear = !cubemap.isDataSRGB;
#endif
			for (int i = 0; i < 6; i++)
			{
				var face = (CubemapFace)i;
				Color[] pixels = readableCubeMap.GetPixels(face, 0);
				pixels = TextureUtility.VertialInvertPixels(pixels, texWidth, texWidth);

				Texture2D readableTexture = new Texture2D(texWidth, texWidth, textureFormat: textureFormat,
					mipChain: (cubemap.mipmapCount > 1), linear: isLinear);
				readableTexture.SetPixels(0, 0, texWidth, texWidth, pixels, 0);
				_cachedFaces[i] = readableTexture;
			}

			RenderTexture.ReleaseTemporary(tempRT);

			DestroyImmediate(readableCubeMap);
			_isDone = true;
		}
	}
}