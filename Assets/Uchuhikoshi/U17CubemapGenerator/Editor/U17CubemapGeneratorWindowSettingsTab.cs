using UnityEngine;
using UnityEditor;

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public sealed class U17CubemapGeneratorWindowSettingsTab : U17CubemapGeneratorWindowTabBase
	{
		readonly string[] _languageOptions = null!;

		public U17CubemapGeneratorWindowSettingsTab(U17CubemapGeneratorWindowContext context_, IU17CubemapGeneratorWindow window_)
			: base(context_, window_)
		{
			_languageOptions = EnumUtility.StringListOfEnum<SystemLanguage>(context.SupportedLanguages).ToArray();
		}

		public override void OnGUI()
		{
			if (context.HideOtherUI)
			{
				return;
			}

			EditorGUI.BeginChangeCheck();
			int languageIndex = context.SupportedLanguages.IndexOf((x) => x == context.Language);
			languageIndex = EditorGUILayout.Popup(context.GetText(TextId.Language), languageIndex, _languageOptions, GUILayout.Width(220));
			if (EditorGUI.EndChangeCheck())
			{
				context.Language = context.SupportedLanguages[languageIndex];
			}

			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.LabelField(context.PipelineType.ToString(), EditorStyles.helpBox, GUILayout.Width(220));
			EditorGUI.EndDisabledGroup();
		}
	}
}
