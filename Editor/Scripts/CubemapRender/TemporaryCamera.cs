using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     シーンからキューブマップをレンダリングするための非表示キャプチャカメラの生成とライフサイクルを管理するクラス。
    ///     ユーザーのアクティブシーンを汚染しないよう、HideFlags.HideAndDontSave で非表示・非シリアライズ管理されます。
    /// </summary>
    public sealed class TemporaryCamera : IDisposable
    {
        private static readonly Vector3 DefaultRenderCameraPosition = new(0, 1, -10);
        private GameObject _gameObject = null!;

        public TemporaryCamera()
        {
            _gameObject = new GameObject("U17CubeGen.RenderCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _gameObject.transform.position = DefaultRenderCameraPosition;
            _gameObject.transform.rotation = Quaternion.identity;

            Camera = _gameObject.AddComponent<Camera>();
            Camera.enabled = false;
            Camera.depth = -1;
        }

        public Camera Camera { get; private set; } = null!;

        public void Dispose()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
                _gameObject = null!;
                Camera = null!;
            }
        }

        public void ResetTransform()
        {
            if (Camera != null)
            {
                Camera.transform.SetPositionAndRotation(DefaultRenderCameraPosition, Quaternion.identity);
            }
        }
    }
}
