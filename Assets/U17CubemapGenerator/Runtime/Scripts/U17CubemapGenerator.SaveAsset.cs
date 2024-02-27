using System;
using System.Collections;
using System.Reflection.Emit;
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
		bool _isSaveProcessing;

		interface ICubemapSave : IDisposable
		{
			public IEnumerator SaveAsPNGCoroutine(string assetPath);
		}

		abstract class CubemapSaveBase : ICubemapSave
		{
			protected U17CubemapGenerator generator { get; private set; }

			protected CubemapSaveBase(U17CubemapGenerator generator)
			{
				this.generator = generator;
			}

			public abstract IEnumerator SaveAsPNGCoroutine(string assetPath);

			public virtual void Dispose()
			{
				this.generator = null!;
   			}
		}

		public void SaveAsset(string assetPath, Action<string>? onCompleted = null)
		{
			_isOutputHDR = _isSourceHDR && _isOutputDesirableHDR;
			_isOutputSRGB = _isOutputDesirableSRGB;
			_isOutputCubemap = _isOutputDesirableCubemap;
			_isOutputGenerateMipmap = _isOutputDesirableGenerateMipmap;

			string ext = _isOutputHDR ? "exr" : "png";
			assetPath = System.IO.Path.ChangeExtension(assetPath, ext);

			if (string.IsNullOrEmpty(assetPath))
			{
				assetPath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
				if (!string.IsNullOrEmpty(assetPath))
				{
					assetPath = System.IO.Path.GetDirectoryName(assetPath);
				}
				if (string.IsNullOrEmpty(assetPath))
				{
					assetPath = "Assets";
				}
			}

			var fileName = "cubemap." + ext;
			fileName = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(assetPath, fileName)));
			assetPath = EditorUtility.SaveFilePanelInProject("Save Cubemap", fileName, ext, "", assetPath);

			if (!string.IsNullOrEmpty(assetPath))
			{
				SaveCubemapAsPNG(assetPath, onCompleted);
			}
		}

		void SaveCubemapAsPNG(string assetPath, Action<string>? onCompleted)
		{
			_isSaveProcessing = true;
			StartCoroutine(SaveProcessCoroutine(assetPath, onCompleted));
		}

		IEnumerator SaveProcessCoroutine(string assetPath, Action<string>? onCompleted)
		{
			try
			{
				yield return StartCoroutine(SaveProcessMainCoroutine(assetPath, onCompleted));
			}
			finally
			{
				_isSaveProcessing = false;
				_rendererCamera.gameObject.SetActive(false);
			}
		}

		IEnumerator SaveProcessMainCoroutine(string assetPath, Action<string>? onCompleted)
		{
			_isDone = false;

			ICubemapSave cubemapSave;
			switch (_outputLayout)
			{
				case OutputLayout.CrossHorizontal:
				case OutputLayout.CrossVertical:
				case OutputLayout.StraitHorizontal:
				case OutputLayout.StraitVertical:
					cubemapSave = new SaveAsCubemap(this);
					break;
				case OutputLayout.SixSided:
					cubemapSave = new SaveAs6Sided(this);
					break;
				case OutputLayout.Equirectanglar:
					cubemapSave = new SaveAsEquirectangular(this);
					break;
				case OutputLayout.Matcap:
					cubemapSave = new SaveAsMatcap(this);
					break;
				default:
					throw new InvalidOperationException("Unsupported OutputLayout type");
			}

			try
			{
				yield return StartCoroutine(cubemapSave.SaveAsPNGCoroutine(assetPath));
				onCompleted?.Invoke(assetPath);
			}
			finally
			{
				cubemapSave.Dispose();
			}
			yield break;
		}

		static void SetOutputSpecification(string assetPath, TextureImporterShape shape, bool generateMip, bool sRGB)
		{
			var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
			if (textureImporter != null)
			{
				bool modified = false;
				if (textureImporter.textureShape != shape)
				{
					textureImporter.textureShape = shape;
					modified = true;
				}
				if (textureImporter.mipmapEnabled != generateMip)
				{
					textureImporter.mipmapEnabled = generateMip;
					modified = true;
				}
				if (textureImporter.sRGBTexture != sRGB)
				{
					textureImporter.sRGBTexture = sRGB;
					modified = true;
				}
				if (modified)
				{
					EditorUtility.SetDirty(textureImporter);
					AssetDatabase.ImportAsset(assetPath);
				}
			}
		}
#endif
	}
}