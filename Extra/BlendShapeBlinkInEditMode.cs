using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRoidTuner
{

    [ExecuteAlways]
    public class BlendShapeBlinkInEditMode : SceneViewRenderer
    {

        internal override void OnAwakeInEditor()
        {
            var blink = GetComponent<BlendShapeBlink>();
            if (blink != null) blink.ResetBlinking();
        }

        internal override void OnDestroyInEditor()
        {
            var blink = GetComponent<BlendShapeBlink>();
            if (blink != null)
            {
                blink.ResetBlinking();
                blink.LateUpdate();
            }
        }

        internal override void OnInitializeOnLoad()
        {
            var blink = GetComponent<BlendShapeBlink>();
            if (blink != null) blink.ResetBlinking();
        }

        internal override void OnFixedUpdateInEditor(SceneView view)
        {
            var blink = GetComponent<BlendShapeBlink>();
            if (blink != null) blink.LateUpdate();
        }

    }

}
