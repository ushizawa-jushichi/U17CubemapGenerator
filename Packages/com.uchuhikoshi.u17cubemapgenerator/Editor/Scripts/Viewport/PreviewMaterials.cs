using System;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     プレビュー表示用マテリアル（Skybox、Sphere、Cube）の生成、更新、破棄を管理するクラス。
    /// </summary>
    public sealed class PreviewMaterials : IDisposable
    {
        private static readonly int IDMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int IDTex = Shader.PropertyToID("_Tex");

        private readonly Settings _settings;

        public PreviewMaterials(Settings settings)
        {
            _settings = settings;
            Assert.IsNotNull(_settings, $"{nameof(_settings)} is not attached.");

            try
            {
                CreateMaterials();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public Material MatSkybox { get; private set; } = null!;
        public Material MatSphere { get; private set; } = null!;
        public Material MatCube { get; private set; } = null!;

        public void Dispose()
        {
            SafeDestroy(MatSkybox);
            MatSkybox = null!;

            SafeDestroy(MatSphere);
            MatSphere = null!;

            SafeDestroy(MatCube);
            MatCube = null!;
        }

        public void ApplyCubemap(Texture cubemap)
        {
            if (MatSkybox == null || MatSphere == null || MatCube == null)
            {
                return;
            }

            MatSkybox.SetTexture(IDTex, cubemap);
            MatSphere.SetTexture(IDMainTex, cubemap);
            MatCube.SetTexture(IDMainTex, cubemap);
        }

        private void CreateMaterials()
        {
            MatSphere = new Material(_settings.MatPreview)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "U17CubeGen.MatSphere"
            };
            MatSphere.EnableKeyword("ROTATE_ON");

            MatCube = new Material(_settings.MatPreview)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "U17CubeGen.MatCube"
            };
            MatCube.EnableKeyword("SKYBOX_ON");

            MatSkybox = new Material(_settings.MatSkybox)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "U17CubeGen.MatSkybox"
            };
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
