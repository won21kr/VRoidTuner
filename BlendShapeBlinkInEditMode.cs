using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRoidTuner
{

    [ExecuteAlways]
    public class BlendShapeBlinkInEditMode : MonoBehaviour
    {

        double LastTimestamp;
        float SpentTimeSinceLastFrame;
        const float FrameTime = 1f/60f;

        void Awake()
        {
            LastTimestamp = EditorApplication.timeSinceStartup;
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            var blink = GetComponent<BlendShapeBlink>();
            if (blink != null) blink.ResetBlinking();
        }

        void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void OnSceneGUI(SceneView view)
        {
            if (Application.IsPlaying(gameObject)) return;

            var timestamp = EditorApplication.timeSinceStartup;
            var deltaSec = (float)(timestamp - LastTimestamp);
            LastTimestamp = timestamp;

            SpentTimeSinceLastFrame += deltaSec;
            if (FrameTime <= SpentTimeSinceLastFrame)
            {
                SpentTimeSinceLastFrame %= FrameTime;
                var blink = GetComponent<BlendShapeBlink>();
                if (blink != null) blink.LateUpdate();
            }
        }

    }

}
