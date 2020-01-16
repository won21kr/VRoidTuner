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
                if (!root.name.StartsWith("HairJoint-")) continue;

                // 子孫オブジェクトを先端まで取得
                var cmps = new List<Transform>();
                for (var cmp = root; cmp != null; cmp = GetFirstChild(cmp))
                {
                    cmps.Add(cmp);
                }

                var step = angle / cmps.Count;
                var a = 0f;
                foreach (var cmp in cmps)
                {
                    a += step;
                    var e = cmp.eulerAngles;
                    e.y = a;
                    cmp.eulerAngles = e;
                    EditorUtility.SetDirty(cmp);
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
