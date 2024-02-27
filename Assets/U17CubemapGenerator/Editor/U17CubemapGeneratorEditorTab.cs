using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
    public interface IU17CubemapGeneratorEditorTabView
    {
        void OnEnable();
        void OnDisable();
        void OnDestroy();
        void OnGUI();
        void OnUpdate(bool isTabActive);
    }

    public interface ICubemapGeneratorPreviewSceneRenderer
    {
        void OnGUIFirst();
    }
	
    public abstract class U17CubemapGeneratorEditorTabBase : IU17CubemapGeneratorEditorTabView
    {
        U17CubemapGeneratorEditorContext _context = null!;
        protected U17CubemapGeneratorEditorContext context => _context;

        IU17CubemapGeneratorEditor _editor = null!;
        protected IU17CubemapGeneratorEditor editor => _editor;

        protected U17CubemapGeneratorEditorTabBase(U17CubemapGeneratorEditorContext context, IU17CubemapGeneratorEditor editor)
        {
            _context = context;
            _editor = editor;
        }

        public virtual void OnEnable() {}
        public virtual void OnDisable() {}
        public virtual void OnDestroy()
        {
            _context = null!;
            _editor = null!;
        }
        public virtual void OnUpdate(bool isTabActive) {}
        public virtual void OnGUI() {}
    }
}
