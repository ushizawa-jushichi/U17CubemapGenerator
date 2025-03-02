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
#if UNITY_EDITOR
	public partial class U17CubemapGenerator : MonoBehaviour, IU17CubemapGenerator
	{
		public void SetCubemapOutputEquirectangularRotation(float angleEulerY)
		{
			if (this.IsProcessing) { throw new InvalidOperationException("Now processing"); }
			_equirectangularRotationEulerY = angleEulerY;
		}

		class SaveAsEquirectangular : CubemapSaveBase
		{
			public SaveAsEquirectangular(U17CubemapGenerator generator) : base(generator) { }

			public override IEnumerator SaveAsPNGCoroutine(string assetPath)
			{
				if (generator._materialBlitter == null) throw new InvalidOperationException($"{nameof(generator._materialBlitter)} null");

				var targetSourceCamera = generator.GetTargetCamera();
				generator._rendererCamera.CopyFrom(targetSourceCamera);
				generator._rendererCamera.gameObject.SetActive(true);

				Texture? texCubemap = null;
				if (generator._cubemapRT != null)
				{
					texCubemap = generator._cubemapRT;
				}
				else if (generator._cubemapAlter != null)
				{
					texCubemap = generator._cubemapAlter;
				}
				else
				{
					texCubemap = generator._texCubemap;
				}
				generator._materialBlitter.SetTexture(_idCubeTex, texCubemap);
				generator._materialBlitter.SetFloat(_idRotationY, generator._equirectangularRotationEulerY * Mathf.Deg2Rad);

				var desc = generator.GetRenderTextureDescriptorForOutputTemporary(generator._textureWidth * 2, generator._textureWidth, false);
				var tempRT = RenderTexture.GetTemporary(desc);

				Graphics.SetRenderTarget(tempRT, 0, 0);
				Graphics.Blit(tempRT, generator._materialBlitter, pass: 1);

				RenderTexture previous = RenderTexture.active;
				RenderTexture.active = tempRT;

				var tempTex = generator.CreateTexture2DForOutputTemporary(tempRT.width, tempRT.height);
				tempTex.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
				tempTex.Apply();

				var bytes = tempTex.EncodeToPNG();   
				File.WriteAllBytes(assetPath, bytes);      

				RenderTexture.active = previous;
				RenderTexture.ReleaseTemporary(tempRT);
				AssetDatabase.ImportAsset(assetPath);
				U17CubemapGenerator.SetOutputSpecification(assetPath,
					(generator._isOutputCubemap ? TextureImporterShape.TextureCube : TextureImporterShape.Texture2D),
					generator._isOutputGenerateMipmap,
					generator._isOutputSRGB);
				AssetDatabase.Refresh();

				yield break;
			}
		}
	}
#endif
}