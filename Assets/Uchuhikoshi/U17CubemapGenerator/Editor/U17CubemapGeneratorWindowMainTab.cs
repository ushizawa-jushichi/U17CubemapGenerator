using UnityEngine;
using UnityEditor;

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public sealed class U17CubemapGeneratorWindowMainTab : U17CubemapGeneratorWindowTabBase
	{
		string[] _inputSourceOptions = null!;
		string[] _outputLayoutOptions = null!;
		string[] _textureWidthOptions = null!;
		string[] _textureShapeOptions = null!;
		string[] _rotationAnglesOptions = null!;

		public U17CubemapGeneratorWindowMainTab(U17CubemapGeneratorWindowContext context_, IU17CubemapGeneratorWindow window_)
			: base(context_, window_)
		{
			BuildOptionStringList();
			context.OnLanguageChanged += (value) => BuildOptionStringList();
		}

		void BuildOptionStringList()
		{
			//_inputSourceOptions = EnumUtils.StringListOfEnum<U17CubemapGenerator.InputSource>().ToArray();
			_inputSourceOptions = new string[] {
				context.GetText(TextId.InputSourceCurrentScene),
				context.GetText(TextId.InputSourceSixSided),
				context.GetText(TextId.InputSourceCubemap),
			};

			//_outputLayoutOptions = EnumUtils.StringListOfEnum<U17CubemapGenerator.OutputLayout>().ToArray();
			_outputLayoutOptions = new string[]
			{
				context.GetText(TextId.OutputLayoutCrossHorizontal),
				context.GetText(TextId.OutputLayoutCrossVertical),
				context.GetText(TextId.OutputLayoutStraitHorizontal),
				context.GetText(TextId.OutputLayoutStraitVertical),
				context.GetText(TextId.OutputLayoutSixSided),
				context.GetText(TextId.OutputLayoutEquirectangular),
				context.GetText(TextId.OutputLayoutMatcap),
			};

			_textureWidthOptions = EnumUtility.StringListOfEnum<U17CubemapGeneratorWindowContext.TextureWidthType>().ToArray();
			Replace(_textureWidthOptions, "_", string.Empty);

			_textureShapeOptions = new string[]
			{
				context.GetText(TextId.TextureShape2D),
				context.GetText(TextId.TextureShapeCube),
			};

			_rotationAnglesOptions = EnumUtility.StringListOfEnum<U17CubemapGeneratorWindowContext.RotationAngleType>().ToArray();
			Replace(_rotationAnglesOptions, "_", string.Empty);
		}

		public override void OnDestroy()
		{
			_inputSourceOptions = null!;
			_outputLayoutOptions = null!;
			_textureWidthOptions = null!;
			_textureShapeOptions = null!;
			_rotationAnglesOptions = null!;
		}

		public override void OnGUI()
		{
			base.OnGUI();
			
			if (context.HideOtherUI)
			{
				return;
			}
			EditorGUI.BeginChangeCheck();
			int inputSourceIndex = EnumUtility.IndexOfEnum(context.InputSource);
			inputSourceIndex = EditorGUILayout.Popup(context.GetText(TextId.SelectInputSource), inputSourceIndex, _inputSourceOptions, GUILayout.Width(270));
			if (EditorGUI.EndChangeCheck())
			{
				context.InputSource = EnumUtility.EnumByIndex<U17CubemapGenerator.InputSource>(inputSourceIndex);
			}

			if (!string.IsNullOrEmpty(context.AdviceMessage))
			{
				EditorGUILayout.LabelField(context.AdviceMessage, EditorStyles.helpBox, GUILayout.Width(420));
			}

			switch (context.InputSource)
			{
				case U17CubemapGenerator.InputSource.CurrentScene:
					int textureWidthIndex = EnumUtility.IndexOfEnum(context.TextureWidth);
					textureWidthIndex = EditorGUILayout.Popup(context.GetText(TextId.InputTextureWidth), textureWidthIndex, _textureWidthOptions, GUILayout.Width(270));
					if (EditorGUI.EndChangeCheck())
					{
						context.TextureWidth = EnumUtility.EnumByIndex<U17CubemapGeneratorWindowContext.TextureWidthType>(textureWidthIndex);
					}
					context.SpecificCamera = EditorGUILayout.ObjectField(context.GetText(TextId.SpecificCamera), context.SpecificCamera, typeof(Camera), true, GUILayout.Width(220)) as Camera;
					context.UsingCameraAngles = EditorGUILayout.Toggle(context.GetText(TextId.UsingCameraAngles), context.UsingCameraAngles, GUILayout.Width(220));

					int anglesIndex = EnumUtility.IndexOfEnum(context.HorizontalRotation);
					anglesIndex = EditorGUILayout.Popup(context.GetText(TextId.HorizontalRotation), anglesIndex, _rotationAnglesOptions, GUILayout.Width(270));
					if (EditorGUI.EndChangeCheck())
					{
						context.HorizontalRotation = EnumUtility.EnumByIndex<U17CubemapGeneratorWindowContext.RotationAngleType>(anglesIndex);
					}
					break;

				case U17CubemapGenerator.InputSource.Cubemap:
					context.Cubemap = EditorGUILayout.ObjectField(context.GetText(TextId.InputCubemap), context.Cubemap, typeof(Cubemap), false, GUILayout.Width(220)) as Cubemap;
					break;

				case U17CubemapGenerator.InputSource.SixSided:
					GUILayout.BeginHorizontal();
					context.TextureLeft = EditorGUILayout.ObjectField(context.GetText(TextId.InputLeft), context.TextureLeft, typeof(Texture2D), false, GUILayout.Width(220)) as Texture2D;
					context.TextureRight = EditorGUILayout.ObjectField(context.GetText(TextId.InputRight), context.TextureRight, typeof(Texture2D), false, GUILayout.Width(220)) as Texture2D;
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					context.TextureTop = EditorGUILayout.ObjectField(context.GetText(TextId.InputTop), context.TextureTop, typeof(Texture2D), false, GUILayout.Width(220)) as Texture2D;
					context.TextureBottom = EditorGUILayout.ObjectField(context.GetText(TextId.InputBottom), context.TextureBottom, typeof(Texture2D), false, GUILayout.Width(220)) as Texture2D;
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					context.TextureFront = EditorGUILayout.ObjectField(context.GetText(TextId.InputFront), context.TextureFront, typeof(Texture2D), false, GUILayout.Width(220)) as Texture2D;
					context.TextureBack = EditorGUILayout.ObjectField(context.GetText(TextId.InputBack), context.TextureBack, typeof(Texture2D), false, GUILayout.Width(220)) as Texture2D;
					GUILayout.EndHorizontal();
					break;
			}

#if USING_HDRP
			if (context.PipelineType == RenderPipelineUtility.PipelineType.HDPipeline)
			{
				context.ExposureOverride = EditorGUILayout.Toggle(context.GetText(TextId.ExposureOverride), context.ExposureOverride, GUILayout.Width(220));
				EditorGUI.BeginDisabledGroup(!(context.CanRender() && context.ExposureOverride));
				context.FixedExposure = EditorGUILayout.FloatField(context.GetText(TextId.FixedExposure), context.FixedExposure, GUILayout.Width(220));
				context.Compensation = EditorGUILayout.FloatField(context.GetText(TextId.Compensation), context.Compensation, GUILayout.Width(220));
				EditorGUI.EndDisabledGroup();
			}
#endif

			EditorGUI.BeginDisabledGroup(!context.CanRender());
			if (GUILayout.Button(context.GetText(TextId.Redraw), GUILayout.Width(120)))
			{
				context.RequestRedraw();
			}
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.Separator();

			EditorGUI.BeginChangeCheck();
			int outputLayoutIndex = EnumUtility.IndexOfEnum(context.OutputLayout);
			outputLayoutIndex = EditorGUILayout.Popup(context.GetText(TextId.SelectOutputLayout), outputLayoutIndex, _outputLayoutOptions, GUILayout.Width(320));
			if (EditorGUI.EndChangeCheck())
			{
				context.OutputLayout = EnumUtility.EnumByIndex<U17CubemapGenerator.OutputLayout>(outputLayoutIndex);
			}

			EditorGUI.BeginDisabledGroup(!context.IsSourceHDR);
			context.OutputHDR = EditorGUILayout.Toggle(context.GetText(TextId.OutputFormatHDR), context.OutputHDR, GUILayout.Width(220));
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(!U17CubemapGenerator.IsOutputLayoutAvailableBeCubemap(context.OutputLayout));

			EditorGUI.BeginChangeCheck();
			int textureShapeIndex = context.OutputCubemap ? 1 : 0;
			textureShapeIndex = EditorGUILayout.Popup(context.GetText(TextId.TextureShape), textureShapeIndex, _textureShapeOptions, GUILayout.Width(320));
			if (EditorGUI.EndChangeCheck())
			{
				context.OutputCubemap = (textureShapeIndex != 0);
			}
			EditorGUI.EndDisabledGroup();

			context.OutputSRGB = EditorGUILayout.Toggle(context.GetText(TextId.OutputSRGB), context.OutputSRGB, GUILayout.Width(220));
			context.OutputGenerateMipmap = EditorGUILayout.Toggle(context.GetText(TextId.OutputGenerateMipmap), context.OutputGenerateMipmap, GUILayout.Width(220));

			switch (context.OutputLayout)
			{
				case U17CubemapGenerator.OutputLayout.Equirectanglar:
					context.EquirectangularRotation = (float)EditorGUILayout.IntField(context.GetText(TextId.EquirectangularRotation), (int)context.EquirectangularRotation, GUILayout.Width(220));
					break;
				case U17CubemapGenerator.OutputLayout.Matcap:
					context.FillMatcapOutsideByEdgeColor = EditorGUILayout.Toggle(context.GetText(TextId.FillMatcapOutsideByEdgeColor), context.FillMatcapOutsideByEdgeColor, GUILayout.Width(220));
					break;
			}

			if (GUILayout.Button(context.GetText(TextId.Export), GUILayout.Width(120)))
			{
				context.ExportSaveCubemap();
			}
		}

		static void Replace(string[] strings, string oldValue, string newValue)
		{
			for (int i = 0; i < strings.Length; i++)
			{
				strings[i] = strings[i].Replace(oldValue, newValue);
			}
		}
	}
}
