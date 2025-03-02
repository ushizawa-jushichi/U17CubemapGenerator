using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public sealed class U17CubemapGeneratorWindow : EditorWindow, IU17CubemapGeneratorWindow
	{
		EditorWindow IU17CubemapGeneratorWindow.Window => this;

		int _tabIndex;
		U17CubemapGeneratorWindowContext? _context = null!;

		readonly List<string> _tabNameList = new List<string>();
		readonly List<IU17CubemapGeneratorWindowTabView> _tabViewList = new List<IU17CubemapGeneratorWindowTabView>();

		Rect _mainViewRect = new Rect();
		public Rect MainViewRect => _mainViewRect;

		[MenuItem("Tools/U17CubemapGenerator", false, 1)]
		static void Create()
		{
			var window = GetWindow<U17CubemapGeneratorWindow>();
		}

		void Initialize()
		{
			titleContent = new GUIContent("U17CubemapGenerator");
			_context = new U17CubemapGeneratorWindowContext();

			this.wantsMouseMove = true;

			BuildOptionStringList();
			_context.OnLanguageChanged += (_) => BuildOptionStringList();

			_tabViewList.Add(new U17CubemapGeneratorWindowMainTab(_context, this));
			_tabViewList.Add(new U17CubemapGeneratorWindowPreviewTab(_context, this));
			_tabViewList.Add(new U17CubemapGeneratorWindowSettingsTab(_context, this));
		}

		void BuildOptionStringList()
		{
			if (_context == null) { throw new InvalidOperationException(); }
			_tabNameList.Clear();
			_tabNameList.Add(_context.GetText(TextId.TabMain));
			_tabNameList.Add(_context.GetText(TextId.TabPreview));
			_tabNameList.Add(_context.GetText(TextId.TabSettings));
		}

		void OnEnable()
		{
			if (_context == null)
			{
				Initialize();
			}
			foreach (var tab in _tabViewList) { tab.OnEnable(); }
		}

		void OnDisable()
		{
			foreach (var tab in _tabViewList) { tab.OnDisable(); }
		}

		void OnDestroy()
		{
			foreach (var tab in _tabViewList) { tab.OnDestroy(); }
			_tabViewList.Clear();
			_context?.Dispose();
			_context = null!;
		}

		void OnGUI()
		{
			if (_context == null)
			{
				return;
			}

			CalculateViewSize();

			OnGUIManageMouseEvent();

			foreach (var tab in _tabViewList)
			{
				if (tab is IU17CubemapGeneratorPreviewSceneRenderer previewTab)
				{
					previewTab.OnGUIFirst();
				}
			}

			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				_tabIndex = GUILayout.Toolbar(_tabIndex, _tabNameList.ToArray(), new GUIStyle(EditorStyles.toolbarButton), GUI.ToolbarButtonSize.FitToContents);
			}

			OnGUICommon();

			_tabViewList[_tabIndex]?.OnGUI();
		}

		void Update()
		{
			if (_context == null)
			{
				return;
			}
			_context.OnUpdate();

			for (int i = 0; i < _tabViewList.Count; i++)
			{
				_tabViewList[i].OnUpdate(_tabIndex == i);
			}
		}

		void OnGUIManageMouseEvent()
		{
			if (Event.current == null)
			{
				return;
			}
			if (_context == null) { throw new InvalidOperationException(); }
			if (_context.Generator == null) { throw new InvalidOperationException(); }

			if (Event.current.type == EventType.MouseMove ||
				Event.current.type == EventType.MouseDrag)
			{
				_context.Generator.SetEditorMousePosition(Event.current.mousePosition);
			}
			if (Event.current.type == EventType.MouseDown)
			{
				_context.Generator.SetEditorMouseDown();
			}
			if (Event.current.type == EventType.MouseMove ||
				Event.current.type == EventType.MouseUp)
			{
				_context.Generator.SetEditorMouseUp();
			}
		}

		void OnGUICommon()
		{
			if (_context == null) { throw new InvalidOperationException(); }
			
			_context.HideOtherUI = EditorGUILayout.Toggle(_context.GetText(TextId.HideOtherUIs), _context.HideOtherUI);
			if (_context.HideOtherUI)
			{
				return;
			}
		}

		void CalculateViewSize()
		{
			float toolbarHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;

			_mainViewRect = new Rect(0f, toolbarHeight, this.position.width, this.position.height - toolbarHeight);
		}
	}
}
