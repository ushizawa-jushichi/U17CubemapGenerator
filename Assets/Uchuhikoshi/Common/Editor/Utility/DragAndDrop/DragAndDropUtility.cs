using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#nullable enable

namespace Uchuhikoshi
{
    public static class DragAndDropUtility
    {
        public static void Begin(Object[] objs, object data)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.paths = null;
            DragAndDrop.objectReferences = objs;
            DragAndDrop.SetGenericData("data", data);
            DragAndDrop.StartDrag(objs.Length == 1 ? objs[0].name : "<Multiple>");
        }
    
        public static bool Check(EditorWindow window, Rect? rect = null)
        {
            var ev = Event.current;
            if (rect.HasValue && !rect.Value.Contains(ev.mousePosition))
                return false;
 
            if (ev.type != EventType.DragUpdated && ev.type != EventType.DragPerform)
                return false;
 
            var paths = DragAndDrop.paths;
            if (EditorWindow.mouseOverWindow != window || paths.Length <= 0)
                return false;
                
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (ev.type == EventType.DragPerform)
            {
                DragAndDrop.activeControlID = 0;
                DragAndDrop.AcceptDrag();
                return true;
            }
 
            DragAndDrop.activeControlID = GUIUtility.GetControlID(FocusType.Passive);
            Event.current.Use();
 
            return false;
        }
    }
}
