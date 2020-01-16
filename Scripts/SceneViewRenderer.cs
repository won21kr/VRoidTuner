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
        internal abstract void OnInitializeOnLoad();
        internal abstract void OnFixedUpdateInEditor(SceneView view);

        void Awake()
        {
            if (Application.IsPlaying(gameObject)) return;
            LastTimestamp = EditorApplication.timeSinceStartup;
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            OnAwakeInEditor();
        }

        void OnDestroy()
        {
            if (Application.IsPlaying(gameObject)) return;
            OnDestroyInEditor();
            EditorApplication.update -= OnEditorUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        [InitializeOnLoadMethod]
        void InitializeOnLoad()
        {
            OnInitializeOnLoad();
        }

        void OnEditorUpdate()
        {
            if (Application.IsPlaying(gameObject)) return;
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
            if (Application.IsPlaying(gameObject)) return;
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
