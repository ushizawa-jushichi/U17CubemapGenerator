using UnityEngine;

#nullable enable

namespace Uchuhikoshi
{
	public static class TextureUtility
	{
		public static Texture2D CreateReadabeTexture2D(Texture2D texture2d)
		{
			bool isLinear = false;
#if UNITY_2022_1_OR_NEWER
			isLinear = !texture2d.isDataSRGB;
#endif
			RenderTexture renderTexture = RenderTexture.GetTemporary(
				texture2d.width,
				texture2d.height,
				0,
				RenderTextureFormat.Default,
				(isLinear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB));

			Graphics.Blit(texture2d, renderTexture);
			RenderTexture previous = RenderTexture.active;
			RenderTexture.active = renderTexture;
			TextureFormat textureFormat = texture2d.format.IsHDRFormat() ? TextureFormat.RGBAHalf : TextureFormat.RGBA32;
			Texture2D readableTextur2D = new Texture2D(texture2d.width, texture2d.height, textureFormat, mipCount: texture2d.mipmapCount, linear: isLinear);
			readableTextur2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
			readableTextur2D.Apply();
			RenderTexture.active = previous;
			RenderTexture.ReleaseTemporary(renderTexture);
			return readableTextur2D;
		}

		public static void FillTexture2D(Texture2D texture2d, Color color)
		{
			int width = texture2d.width;
			int height = texture2d.height;
			int elements = width * height;
			Color[] pixels = new Color[elements];
			for (int i = 0; i < elements; i++)
			{
				pixels[i] = color;
			}
			texture2d.SetPixels(pixels);
		}

		public static Color[] VertialInvertPixels(Color[] pixels, int blockWidth, int blockHeight)
		{
			Color[] pixels2 = new Color[pixels.Length];

			for (int y = 0; y < blockHeight; y++)
			{
				for (int x = 0; x < blockWidth; x++)
				{
					pixels2[y * blockHeight + x] = pixels[(blockHeight - 1 - y) * blockHeight + x];
				}
			}
			return pixels2;
		}
	}
}
