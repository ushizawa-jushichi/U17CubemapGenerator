using UnityEngine;
using UnityEditor;

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public sealed class U17CubemapGeneratorEditorMainTab : U17CubemapGeneratorEditorTabBase
	{
		string[] _inputSourceOptions = null!;
		string[] _outputLayoutOptions = null!;
		string[] _textureWidthOptions = null!;
		string[] _textureShapeOptions = null!;
		string[] _rotationAnglesOptions = null!;

		public U17CubemapGeneratorEditorMainTab(U17CubemapGeneratorEditorContext context, IU17CubemapGeneratorEditor editor)
			: base(context, editor)
		{
			BuildOptionStringList();
			context.onLanguageChanged += (value) => BuildOptionStringList();
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

			_textureWidthOptions = EnumUtils.StringListOfEnum<U17CubemapGeneratorEditorContext.TextureWidthType>().ToArray();
			Replace(_textureWidthOptions, "_", string.Empty);

			_textureShapeOptions = new string[]
			{
				context.GetText(TextId.TextureShape2D),
				context.GetText(TextId.TextureShapeCube),
			};

			_rotationAnglesOptions = EnumUtils.StringListOfEnum<U17CubemapGeneratorEditorContext.RotationAngleType>().ToArray();
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
			
			if (context.hideOtherUI)
			{
				return;
			}
			EditorGUI.BeginChangeCheck();
			int inputSourceIndex = EnumUtils.IndexOfEnum(context.inputSource);
			inputSourceIndex = EditorGUILayout.Popup(context.GetText(TextId.SelectInputSource), inputSourceIndex, _inputSourceOptions, GUILayout.Width(270));
			if (EditorGUI.EndChangeCheck())
			{
				context.inputSource = EnumUtils.EnumByIndex<U17CubemapGenerator.InputSource>(inputSourceIndex);
			}

			if (!string.IsNullOrEmpty(context.adviceMessage))
			{
				EditorGUILayout.LabelField(context.adviceMessage, EditorStyles.helpBox, GUILayout.Width(420));
			}

			switch (context.inputSource)
			{
				case U17CubemapGenerator.InputSource.CurrentScene:
					int textureWidthIndex = EnumUtils.IndexOfEnum(context.textureWidth);
					textureWidthIndex = EditorGUILayout.Popup(context.GetText(TextId.InputTextureWidth), textureWidthIndex, _textureWidthOptions, GUILayout.Width(270));
					if (EditorGUI.EndChangeCheck())
					{
						context.textureWidth = EnumUtils.EnumByIndex<U17CubemapGeneratorEditorContext.TextureWidthType>(textureWidthIndex);
					}
					context.specificCamera = EditorGUILayout.ObjectField(context.GetText(TextId.SpecificCamera), context.specificCamera, typeof(Camera), true, GUILayout.Width(220)) as Camera;
					context.usingCameraAngles = EditorGUILayout.Toggle(context.GetText(TextId.UsingCameraAngles), context.usingCameraAngles, GUILayout.Width(220));

					int anglesIndex = EnumUtils.IndexOfEnum(context.horizontalRotation);
					anglesIndex = EditorGUILayout.Popup(context.GetText(TextId.HorizontalRotation), anglesIndex, _rotationAnglesOptions, GUILayout.Width(270));
					if (EditorGUI.EndChangeCheck())
					{
						context.horizontalRotation = EnumUtils.EnumByIndex<U17CubemapGeneratorEditorContext.RotationAngleType>(anglesIndex);
					}
					break;

				case U17CubemapGenerator.InputSource.Cubemap:
					context.cubemap = EditorGUILayout.ObjectField(context.GetText(TextId.InputCubemap), context.cubemap, typeof(Cubemap), false, GUILayout.Width(220)) as Cubemap;
					break;

				case U17CubemapGenerator.InputSource.SixSided:
					GUILayout.BeginHorizontal();
					context.textureLeft = EditorGUILayout.ObjectField(context.GetText(TextId.InputLeft), context.textureLeft, typeof(Texture2D), false, GUILayout.Width(220)) as Texture2D;
					context.textureRight = EditorGUILayout.ObjectField(context.GetText(TextId.InputRight), context.textureRight, typeof(Texture2D), false, GUILayout.Width(220)) as Texture2D;
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					context.textureTop = EditorGUILayout.ObjectField(context.GetText(TextId.InputTop), context.textureTop, typeof(Texture2D), false, GUILayout.Width(220)) as Texture2D;
					context.textureBottom = EditorGUILayout.ObjectField(context.GetText(TextId.InputBottom), context.textureBottom, typeof(Texture2D), false, GUILayout.Width(220)) as Texture2D;
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					context.textureFront = EditorGUILayout.ObjectField(context.GetText(TextId.InputFront), context.textureFront, typeof(Texture2D), false, GUILayout.Width(220)) as Texture2D;
					context.textureBack = EditorGUILayout.ObjectField(context.GetText(TextId.InputBack), context.textureBack, typeof(Texture2D), false, GUILayout.Width(220)) as Texture2D;
					GUILayout.EndHorizontal();
					break;
			}

#if USING_HDRP
			if (context.pipelineType == RenderPipelineUtils.PipelineType.HDPipeline)
			{
				context.exposureOverride = EditorGUILayout.Toggle(context.GetText(TextId.ExposureOverride), context.exposureOverride, GUILayout.Width(220));
				EditorGUI.BeginDisabledGroup(!(context.CanRender() && context.exposureOverride));
				context.fixedExposure = EditorGUILayout.FloatField(context.GetText(TextId.FixedExposure), context.fixedExposure, GUILayout.Width(220));
				context.compensation = EditorGUILayout.FloatField(context.GetText(TextId.Compensation), context.compensation, GUILayout.Width(220));
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
			int outputLayoutIndex = EnumUtils.IndexOfEnum(context.outputLayout);
			outputLayoutIndex = EditorGUILayout.Popup(context.GetText(TextId.SelectOutputLayout), outputLayoutIndex, _outputLayoutOptions, GUILayout.Width(320));
			if (EditorGUI.EndChangeCheck())
			{
				context.outputLayout = EnumUtils.EnumByIndex<U17CubemapGenerator.OutputLayout>(outputLayoutIndex);
			}

			EditorGUI.BeginDisabledGroup(!context.isSourceHDR);
			context.outputHDR = EditorGUILayout.Toggle(context.GetText(TextId.OutputFormatHDR), context.outputHDR, GUILayout.Width(220));
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(!U17CubemapGenerator.IsOutputLayoutAvailableBeCubemap(context.outputLayout));

			EditorGUI.BeginChangeCheck();
			int textureShapeIndex = context.outputCubemap ? 1 : 0;
			textureShapeIndex = EditorGUILayout.Popup(context.GetText(TextId.TextureShape), textureShapeIndex, _textureShapeOptions, GUILayout.Width(320));
			if (EditorGUI.EndChangeCheck())
			{
				context.outputCubemap = (textureShapeIndex != 0);
			}
			EditorGUI.EndDisabledGroup();

			context.outputSRGB = EditorGUILayout.Toggle(context.GetText(TextId.OutputSRGB), context.outputSRGB, GUILayout.Width(220));
			context.outputGenerateMipmap = EditorGUILayout.Toggle(context.GetText(TextId.OutputGenerateMipmap), context.outputGenerateMipmap, GUILayout.Width(220));

			switch (context.outputLayout)
			{
				case U17CubemapGenerator.OutputLayout.Equirectanglar:
					context.equirectangularRotation = (float)EditorGUILayout.IntField(context.GetText(TextId.EquirectangularRotation), (int)context.equirectangularRotation, GUILayout.Width(220));
					break;
				case U17CubemapGenerator.OutputLayout.Matcap:
					context.fillMatcapOutsideByEdgeColor = EditorGUILayout.Toggle(context.GetText(TextId.FillMatcapOutsideByEdgeColor), context.fillMatcapOutsideByEdgeColor, GUILayout.Width(220));
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
