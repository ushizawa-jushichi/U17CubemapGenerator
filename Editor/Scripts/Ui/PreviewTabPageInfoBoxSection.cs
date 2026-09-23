using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     ツール情報、画面サイズ、中間サイズ、AsyncGPUReadback 対応状況等の InfoBox を表示・管理する UI セクション。
    /// </summary>
    public sealed class PreviewTabPageInfoBoxSection : VisualElement
    {
        private readonly InfoItem _infoAsyncGPUReadback;
        private readonly InfoBox _infoBox;
        private readonly InfoItem _infoIntermediateSize;
        private readonly InfoItem _infoScreenSize;
        private readonly InfoItem _infoVersion;
        private readonly InfoItem _infoRenderPipeline;

        public PreviewTabPageInfoBoxSection()
        {
            pickingMode = PickingMode.Ignore;

            _infoBox = new InfoBox();
            Add(_infoBox);

            _infoVersion = new InfoItem("Tool Version:");
            _infoVersion.SetValue("v" + GetPackageVersion());
            _infoBox.Add(_infoVersion);

            _infoScreenSize = new InfoItem("Screen Size:");
            _infoBox.Add(_infoScreenSize);

            _infoRenderPipeline = new InfoItem("RenderPipeline:");
            _infoBox.Add(_infoRenderPipeline);
            UpdateRenderPipeline();

            _infoIntermediateSize = new InfoItem("Intermediate Size:");
            _infoBox.Add(_infoIntermediateSize);

            _infoAsyncGPUReadback = new InfoItem("AsyncGPUReadback:");
            _infoAsyncGPUReadback.SetValue(SystemInfo.supportsAsyncGPUReadback ? "Yes" : "N/A");
            _infoBox.Add(_infoAsyncGPUReadback);
        }

        public void UpdateRenderPipeline()
        {
            var desc = RenderPipelineUtility.IsUniversalRenderPipelineActive() ? "URP" :
                RenderPipelineUtility.IsHighDefinitionRenderPipelineActive() ? "HDRP" :
               "-";
            _infoRenderPipeline.SetValue(desc);
        }

        public void SetIntermediateSize(Vector2Int value)
        {
            _infoIntermediateSize.SetValue(value.x > 0 && value.y > 0 ? $"{value.x}x{value.y}" : "-");
        }

        public void OnGeometryChanged(Vector2 size)
        {
            _infoScreenSize.SetValue($"{Mathf.RoundToInt(size.x)}x{Mathf.RoundToInt(size.y)}");
        }

        private static string GetPackageVersion()
        {
            var packageInfo = PackageInfo.FindForAssembly(typeof(PreviewTabPageInfoBoxSection).Assembly);
            return packageInfo != null ? packageInfo.version : Application.version;
        }
    }
}
