using UnityEngine;
using UnityEditor;

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public sealed class U17CubemapGeneratorWindowPreviewTab : U17CubemapGeneratorWindowTabBase, IU17CubemapGeneratorPreviewSceneRenderer
	{
		string[] _previewObjectOptions = null!;

		public U17CubemapGeneratorWindowPreviewTab(U17CubemapGeneratorWindowContext context_, IU17CubemapGeneratorWindow window_)
			: base(context_, window_)
		{
			BuildOptionStringList();
			context.OnLanguageChanged += (value) => BuildOptionStringList();
		}

		void BuildOptionStringList()
		{
			_previewObjectOptions = new string[]
			{
				context.GetText(TextId.PreviewObjectSphere),
				context.GetText(TextId.PreviewObjectCube),
			};
		}

		void IU17CubemapGeneratorPreviewSceneRenderer.OnGUIFirst()
		{
			var viewRect = window.MainViewRect;
			int width = (int)viewRect.width;
			int height = (int)viewRect.height;
			int superSize = context.SuperSampling ? 2 : 1;
			context.PreviewScene.renderTextureSize = new Vector2Int(width * superSize, height * superSize);
			context.PreviewScene.Render(true);
			GUI.DrawTexture(viewRect, context.PreviewScene.renderTexture);
		}

		public override void OnUpdate(bool isTabActive)
		{
			window.Window.Repaint();

			context.PreviewScene.camera.clearFlags = context.ShowSkyBox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
		}

		public override void OnGUI()
		{
			if (context.HideOtherUI)
			{
				return;
			}
			EditorGUILayout.LabelField("Fps", context.Fps.ToString());

			EditorGUI.BeginChangeCheck();
			int previewObjectIndex = EnumUtility.IndexOfEnum(context.PreviewObject);
			previewObjectIndex = EditorGUILayout.Popup(context.GetText(TextId.PreviewObject), previewObjectIndex, _previewObjectOptions, GUILayout.Width(220));
			if (EditorGUI.EndChangeCheck())
			{
				context.PreviewObject = EnumUtility.EnumByIndex<U17CubemapGeneratorWindowContext.PreviewObjectType>(previewObjectIndex);
			}

			if (GUILayout.Button(context.GetText(TextId.ResetPreviewRotation), GUILayout.Width(160)))
			{
				context.ResetPreviewRotation();
			}

			context.SuperSampling = EditorGUILayout.Toggle(context.GetText(TextId.PreviewSupersampling), context.SuperSampling);
			context.ShowSkyBox = EditorGUILayout.Toggle(context.GetText(TextId.PreviewSkybox), context.ShowSkyBox);
		}
	}
}
