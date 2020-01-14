using UnityEngine;
using UnityEditor;
using VRM;

namespace VRoidTuner
{

    public class SpringBoneHelper : EditorWindow
    {

        [MenuItem("VRM/VRoidTuner/Spring Bones/Show All VRMSpringBone Gizmos", true)]
        private static bool ShowAllVRMSpringBoneGizmosValidate()
        {
            (var selectedInHierarchy, var selectedInProject) = Helper.IsVRMSelected();
            return selectedInHierarchy || selectedInProject;
        }

        [MenuItem("VRM/VRoidTuner/Spring Bones/Show All VRMSpringBone Gizmos", false)]
        private static void ShowAllVRMSpringBoneGizmos()
        {
            ShowOrHideAllVRMSpringBoneGizmos(true);
        }

        [MenuItem("VRM/VRoidTuner/Spring Bones/Hide All VRMSpringBone Gizmos", true)]
        private static bool HideAllVRMSpringBoneGizmosValidate()
        {
            (var selectedInHierarchy, var selectedInProject) = Helper.IsVRMSelected();
            return selectedInHierarchy || selectedInProject;
        }

        [MenuItem("VRM/VRoidTuner/Spring Bones/Hide All VRMSpringBone Gizmos", false)]
        private static void HideAllVRMSpringBoneGizmos()
        {
            ShowOrHideAllVRMSpringBoneGizmos(false);
        }

        private static void ShowOrHideAllVRMSpringBoneGizmos(bool show)
        {
            foreach (var cmp in Helper.FindAllComponents<VRMSpringBone>())
            {
                var ser = new SerializedObject(cmp);
                ser.Update();
                ser.FindProperty("m_drawGizmo").boolValue = show;
                ser.ApplyModifiedProperties();
                EditorUtility.SetDirty(cmp);
            }
        }

    }

}
