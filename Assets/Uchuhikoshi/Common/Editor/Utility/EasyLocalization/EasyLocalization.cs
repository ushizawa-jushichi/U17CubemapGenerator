using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Uchuhikoshi
{
	public class EasyLocalization<TTextId> where TTextId : Enum
	{
		SystemLanguage _language = SystemLanguage.Unknown;
		public SystemLanguage Language
		{
			get => _language;
			set
			{
				_language = value;
				_current = _dictionaries[_language];
			}
		}

		readonly Dictionary<SystemLanguage, Dictionary<TTextId, string>> _dictionaries = new Dictionary<SystemLanguage, Dictionary<TTextId, string>>();
		Dictionary<TTextId, string>? _current;
		readonly Dictionary<TTextId, string>? _failSafe;

		readonly List<SystemLanguage> _supportedLanguages = new List<SystemLanguage>();
		public IReadOnlyList<SystemLanguage> SupportedLanguages => _supportedLanguages;

		public EasyLocalization(string assetPath)
		{
			var files = System.IO.Directory.GetFiles(assetPath + "Editor/Assets/Languages/", "*.csv", System.IO.SearchOption.TopDirectoryOnly);
			foreach ( var file in files )
			{
				var languageName = Path.GetFileNameWithoutExtension(file);

				if (EnumUtility.ParseEnum<SystemLanguage>(languageName, out var language))
				{
					var dict = new Dictionary<TTextId, string>();
					LoadCSV(dict, file);
					_dictionaries.Add(language, dict);
					_supportedLanguages.Add(language);
				}
			}

			if (_dictionaries.ContainsKey(Application.systemLanguage))
			{
				this.Language = Application.systemLanguage;
			}
			_dictionaries.TryGetValue(SystemLanguage.English, out _failSafe);
		}

		public string Get(TTextId id)
		{
			if (_current != null)
			{
				if (_current.TryGetValue(id, out var text))
				{
					return text;
				}
			}
			if (_failSafe != null)
			{
				if (_failSafe.TryGetValue(id, out var text))
				{
					return text;
				}
			}
			return id.ToString();
		}

		void LoadCSV(Dictionary<TTextId, string> dict, string assetPath)
		{
			var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
			if (textAsset == null)
			{
				return;
			}
			StringReader reader = new StringReader(textAsset.text);
			while (reader.Peek() != -1)
			{
				string? line = reader.ReadLine();
				if (line != null)
				{
					var words = line.Split(',');
					if (words.Length >= 2)
					{
						if (EnumUtility.ParseEnum<TTextId>(words[0], out var loc))
						{
							dict.TryAdd(loc, words[1]);
						}
					}
				}
			}
		}
	}
}