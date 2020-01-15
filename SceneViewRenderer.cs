using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRoidTuner
{

    [ExecuteAlways]
    public abstract class SceneViewRenderer : MonoBehaviour
    {

        double LastTimestamp;
        const double FrameTime = 0.02f;
        const int MaxFramesAtOnce = 5;

        internal abstract void OnAwakeInEditor();
        internal abstract void OnDestroyInEditor();
        internal abstract void OnFixedUpdateInEditor(SceneView view);

        void Awake()
        {
            LastTimestamp = EditorApplication.timeSinceStartup;
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            OnAwakeInEditor();
        }

        void OnDestroy()
        {
            OnDestroyInEditor();
            EditorApplication.update -= OnEditorUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void OnEditorUpdate()
        {
            CallFixedUpdate(SceneView.lastActiveSceneView);
        }

        void OnSceneGUI(SceneView view)
        {
            if (Application.IsPlaying(gameObject)) return;
            if (Event.current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
            }
            CallFixedUpdate(view);
        }

        void CallFixedUpdate(SceneView view)
        {
            for (var i=0; i<MaxFramesAtOnce; i++)
            {
                if (EditorApplication.timeSinceStartup - LastTimestamp < FrameTime) return;
                OnFixedUpdateInEditor(view);
                LastTimestamp += FrameTime;
            }
            LastTimestamp = EditorApplication.timeSinceStartup;
        }

    }

}
