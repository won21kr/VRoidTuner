using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRM;

namespace VRoidTuner
{

    public class HairTuner
    {

        internal static void ShowAllVRMSpringBoneGizmos(bool show)
        {
            foreach (var cmp in Helper.FindAllComponentsInSelected<VRMSpringBone>())
            {
                var ser = new SerializedObject(cmp);
                ser.Update();
                ser.FindProperty("m_drawGizmo").boolValue = show;
                ser.ApplyModifiedProperties();
                EditorUtility.SetDirty(cmp);
            }
        }

        internal static void TwistHairJoint(float angle)
        {
            foreach (var root in Selection.GetFiltered<Transform>(SelectionMode.TopLevel))
            {
                Debug.Log(root.name);
                if (!root.name.StartsWith("HairJoint-")) continue;

                // 子孫オブジェクトを先端まで取得
                var cmps = new List<Transform>();
                for (var cmp = root; cmp != null; cmp = GetFirstChild(cmp))
                {
                    cmps.Add(cmp);
                }

                for (var i=cmps.Count-1; 0<=i; i--)
                {
                    var cmp = cmps[i];
                    var e = cmp.localEulerAngles;
                    e.y = angle / cmps.Count;
                    cmp.localEulerAngles = e;
                    EditorUtility.SetDirty(cmp);
                    Debug.Log(cmp.gameObject.name + " = " + e);
                }
            }
        }

        private static Transform GetFirstChild(Transform t)
        {
            foreach (var u in t.GetComponentsInChildren<Transform>())
            {
                if (u.parent == t) return u;
            }
            return null;
        }

    }

}
