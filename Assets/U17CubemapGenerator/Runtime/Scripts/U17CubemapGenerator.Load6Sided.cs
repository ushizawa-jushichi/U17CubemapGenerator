using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public partial class U17CubemapGenerator : MonoBehaviour, IU17CubemapGenerator
	{
		[SerializeField] Texture2D? _tex6SidedLeft;
		[SerializeField] Texture2D? _tex6SidedRight;
		[SerializeField] Texture2D? _tex6SidedTop;
		[SerializeField] Texture2D? _tex6SidedBottom;
		[SerializeField] Texture2D? _tex6SidedFront;
		[SerializeField] Texture2D? _tex6SidedBack;

		public bool isAvailableInspector6Sided =>
				(_tex6SidedLeft != null) && (_tex6SidedRight != null) &&
				(_tex6SidedTop != null) && (_tex6SidedBottom != null) &&
				(_tex6SidedFront != null) && (_tex6SidedBack != null);

#if UNITY_EDITOR
		public void SetSourceTexture(CubemapFace face, Texture2D texture)
		{
			if (this.isProcessing) { throw new InvalidOperationException("Now processing"); }

			switch (face)
			{
				case CubemapFace.PositiveX: _tex6SidedLeft = texture; break;
				case CubemapFace.NegativeX: _tex6SidedRight = texture; break;
				case CubemapFace.PositiveY: _tex6SidedTop = texture; break;
				case CubemapFace.NegativeY: _tex6SidedBottom = texture; break;
				case CubemapFace.PositiveZ: _tex6SidedFront = texture; break;
				case CubemapFace.NegativeZ: _tex6SidedBack = texture; break;
				default: throw new ArgumentException("face:" + face);
			}
		}
#endif

		void VerifyAndSetupLoad6Sided(out int cubemapSize)
		{
			if (_tex6SidedLeft == null ||
				_tex6SidedRight == null ||
				_tex6SidedTop == null ||
				_tex6SidedBottom == null ||
				_tex6SidedFront == null ||
				_tex6SidedBack == null)
			{
				throw new InvalidOperationException($"Set Inspector: {nameof(_tex6SidedLeft)}, {nameof(_tex6SidedRight)}...");
			}
			_6sidedSources[0] = _tex6SidedLeft;
			_6sidedSources[1] = _tex6SidedRight;
			_6sidedSources[2] = _tex6SidedTop;
			_6sidedSources[3] = _tex6SidedBottom;
			_6sidedSources[4] = _tex6SidedFront;
			_6sidedSources[5] = _tex6SidedBack;
			Texture2D tex0 = _6sidedSources[0]!;
			for (int i = 0; i < 6; i++)
			{
				Texture2D tex = _6sidedSources[i]!;
				if (tex.width != tex.height ||
					tex.width != tex0.width ||
					tex.height != tex0.height)
				{
					throw new InvalidOperationException("6-Sided Texture size unmatched.");
				}
			}
			cubemapSize = _tex6SidedLeft.width;
		}

		void Load6Sided()
		{
			if (_materialBlitter == null) { throw new ArgumentException($"{nameof(_materialBlitter)} null"); }

			DisposeCachedFaces();

			_materialBlitter.SetVector("_MainTex_ST_", new Vector4(1f, -1f, 0f, 1f));

			for (int i = 0; i < 6; i++)
			{
				var source = _6sidedSources[i];
				if (source == null) { throw new InvalidOperationException(); }

				var face = (CubemapFace)i;
				_cachedFaces[i] = TextureUtils.CreateReadabeTexture2D(source);
				var tex = _cachedFaces[i];
				Graphics.SetRenderTarget(_cubemapRT, 0, face);
				Graphics.Blit(tex, _materialBlitter, pass: 0);
			}
			_materialBlitter.SetVector("_MainTex_ST_", new Vector4(1f, 1f, 0f, 0f));

			_isDone = true;
		}

		// GetPixels/SetPixels is heavy
		void LoadCubemapFacesFrom6Sided(int texWidth)
		{
			DisposeCachedFaces();
			var format = _rendererCamera.allowHDR ? DefaultFormat.HDR : DefaultFormat.LDR;
			var flags = TextureCreationFlags.None;
			_cubemapAlter = new Cubemap(texWidth, format, flags);
			for (int i = 0; i < 6; i++)
			{
				var source = _6sidedSources[i];
				if (source == null) { throw new InvalidOperationException(); }

				var face = (CubemapFace)i;
				_cachedFaces[i] = TextureUtils.CreateReadabeTexture2D(source);
				var pixels = _cachedFaces[i]!.GetPixels();
				_cubemapAlter.SetPixels(pixels, face, 0);
			}
			_cubemapAlter.Apply();
		}
	}
}