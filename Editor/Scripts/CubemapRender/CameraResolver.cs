using System;
using UnityEditor;
using UnityEngine;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     キューブマップレンダリングに使用するカメラを決定するリゾルバー。
    ///     SceneView同期、指定カメラ、Camera.main、シーン内カメラスキャン（プレビューカメラ除外）の優先順位で解決します。
    /// </summary>
    public static class CameraResolver
    {
        private static Camera[] s_CameraBuffer = new Camera[8];


        /// <summary>
        ///     指定された状態および除外カメラに基づき、レンダリング対象のカメラを取得します。
        /// </summary>
        /// <param name="editorState">エディタ設定状態</param>
        /// <param name="excludedCamera">プレビュー用カメラなど除外対象のカメラ（省略可）</param>
        /// <returns>解決されたカメラ（見つからない場合は null）</returns>
        public static Camera ResolveCurrentCamera(EditorState editorState,
            Camera excludedCamera = null!)
        {
            if (editorState == null)
            {
                return null!;
            }

            // 1. SceneView カメラ同期が有効な場合
            if (editorState.RenderSceneSyncSceneViewCamera)
            {
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null && sceneView.camera != null)
                {
                    return sceneView.camera;
                }
            }

            // 2. ユーザー指定の RenderSceneCamera
            var currentCamera = editorState.RenderSceneCamera;
            if (currentCamera != null && currentCamera != excludedCamera)
            {
                return currentCamera;
            }

            // 3. Camera.main へのフォールバック
            var mainCamera = Camera.main;
            if (mainCamera != null && mainCamera != excludedCamera)
            {
                return mainCamera;
            }

            // 4. シーン内の全カメラスキャン（除外カメラをスキップ）
            var cameraCount = Camera.allCamerasCount;
            if (cameraCount == 0)
            {
                return null!;
            }

            if (s_CameraBuffer.Length < cameraCount)
            {
                Array.Resize(ref s_CameraBuffer, Mathf.NextPowerOfTwo(cameraCount));
            }

            try
            {
                var count = Camera.GetAllCameras(s_CameraBuffer);
                Camera result = null!;

                for (var i = 0; i < count; i++)
                {
                    var cam = s_CameraBuffer[i];
                    if (result == null && cam != null && cam != excludedCamera &&
                        cam.cameraType == CameraType.Game &&
                        cam.isActiveAndEnabled &&
                        (cam.hideFlags & (HideFlags.HideAndDontSave | HideFlags.HideInHierarchy)) == 0)
                    {
                        result = cam;
                    }
                }

                return result!;
            }
            finally
            {
                // 参照リーク（破棄されたシーンカメラの保持によるアンロード阻害）を防止するため全要素を確実にクリア
                Array.Clear(s_CameraBuffer, 0, s_CameraBuffer.Length);
            }
        }
    }
}
