using UnityEngine;
using UnityEditor;

#nullable enable

namespace Ushino17
{
	public sealed class U17CubemapGeneratorEditorSettingsTab : U17CubemapGeneratorEditorTabBase
	{
		readonly string[] _languageOptions = null!;

		public U17CubemapGeneratorEditorSettingsTab(U17CubemapGeneratorEditorContext context, IU17CubemapGeneratorEditor editor)
			: base(context, editor)
		{
			_languageOptions = EnumUtils.StringListOfEnum<SystemLanguage>(context.supportedLanguages).ToArray();
		}

		public override void OnGUI()
		{
			if (context.hideOtherUI)
			{
				return;
			}

			EditorGUI.BeginChangeCheck();
			int languageIndex = context.supportedLanguages.IndexOf((x) => x == context.language);
			languageIndex = EditorGUILayout.Popup(context.GetText(TextId.Language), languageIndex, _languageOptions, GUILayout.Width(220));
			if (EditorGUI.EndChangeCheck())
			{
				context.language = context.supportedLanguages[languageIndex];
			}

			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.LabelField(context.pipelineType.ToString(), EditorStyles.helpBox, GUILayout.Width(220));
			EditorGUI.EndDisabledGroup();
		}
	}
}
