using System;
using UnityEngine;
using UnityEditor;
using VRM;
using System.Linq;
using System.Collections.Generic;

namespace VRMHelper
{

    public class ColliderHelper : EditorWindow
    {

        /// <summary>
        /// 球が作成済みで、かつ表示状態であるときtrueを返します。
        /// </summary>
        private static bool AreCollidersActive()
        {
            (var selectedInHierarchy, var selectedInProject) = Helper.IsVRMSelected();
            if (!(selectedInHierarchy || selectedInProject)) return false;
            foreach (var cmp in Helper.FindAllComponentsInSelected<VRMSpringBoneColliderGroup>())
            {
                if (cmp.gameObject.name != "J_Bip_C_Head") continue;
                var parent = cmp.gameObject.transform.Find("_Colliders_");
                if (parent != null) return parent.gameObject.activeInHierarchy;
            }
            return false;
        }

        /// <summary>
        /// 編集を終了するため、オブジェクトの表示状態を通常の状態に戻します。
        /// </summary>
        private static void RevertVisibility()
        {
            // 隠してあったメッシュを再表示
            var cmps = Helper.FindAllComponentsInSelected<SkinnedMeshRenderer>();
            foreach (var cmp in cmps)
            {
                var obj = cmp.gameObject;
                var isFace = obj.name == "Face";
                var isHair = obj.name.StartsWith("Hair");
                var isBody = obj.name == "Body";
                if (isFace || isHair || isBody) obj.SetActive(true);
                if (obj.name == "_DummyFace_") obj.SetActive(false); // 半透明の顔は非表示
            }

            // 球を非表示
            foreach (var cmp in Helper.FindAllComponentsInSelected<VRMSpringBoneColliderGroup>())
            {
                if (cmp.gameObject.name != "J_Bip_C_Head") continue;
                var parent = cmp.gameObject.transform.Find("_Colliders_");
                if (parent != null)
                {
                    parent.gameObject.SetActive(false);
                }
            }
        }

        [MenuItem("VRM/VRM Helper/Colliders/Begin To Edit Head Colliders", true)]
        private static bool BeginToEditHeadCollidersValid()
        {
            return !AreCollidersActive();
        }

        [MenuItem("VRM/VRM Helper/Colliders/Begin To Edit Head Colliders", false)]
        private static void BeginToEditHeadColliders()
        {
            // 半透明の顔が既にある場合は表示
            var dummyFaceExists = false;
            var cmps = Helper.FindAllComponentsInSelected<SkinnedMeshRenderer>();
            foreach (var cmp in cmps)
            {
                if (cmp.gameObject.name == "_DummyFace_")
                {
                    cmp.gameObject.SetActive(true);
                    dummyFaceExists = true;
                }
            }

            // 球が既にある場合は表示
            var collidersExist = false;
            foreach (var cmp in Helper.FindAllComponentsInSelected<VRMSpringBoneColliderGroup>())
            {
                if (cmp.gameObject.name != "J_Bip_C_Head") continue;
                var parent = cmp.gameObject.transform.Find("_Colliders_");
                if (parent != null)
                {
                    parent.gameObject.SetActive(true);
                    collidersExist = true;
                }
            }

            // 全メッシュを隠し、半透明の顔を作成
            foreach (var cmp in cmps)
            {
                var obj = cmp.gameObject;
                var isFace = obj.name == "Face";
                var isHair = obj.name.StartsWith("Hair");
                var isBody = obj.name == "Body";
                if (isFace && !dummyFaceExists)
                {
                    // 顔を複製
                    var dummyFace = Instantiate(obj);
                    dummyFace.transform.parent = obj.transform.parent;
                    dummyFace.name = "_DummyFace_";
                    dummyFace.hideFlags = HideFlags.DontSave;
                    // 半透明化
                    var mesh = dummyFace.GetComponent<SkinnedMeshRenderer>();
                    for (var i=0; i < mesh.materials.Length; i++)
                    {
                        var mat = mesh.materials[i];
                        if (mat.name.Contains("_Face_") || mat.name.Contains("_FaceMouth_"))
                        {
                            var c = mat.GetColor("_Color");
                            c.a = 0.5f;
                            mat.SetColor("_Color", c);
                            mat.SetFloat("_BlendMode", (float)MToon.RenderMode.Transparent);
                            MToon.Utils.ValidateBlendMode(mat, MToon.RenderMode.Transparent, true);
                        }
                    }
                }
                if (isFace || isHair || isBody) obj.SetActive(false);
            }

            if (!collidersExist)
            {
                foreach (var cmp in Helper.FindAllComponentsInSelected<VRMSpringBoneColliderGroup>())
                {
                    // 頭部ボーンを特定
                    if (cmp.gameObject.name != "J_Bip_C_Head") continue;
                    var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VRMHelper/Materials/VRMHelperGizmoMaterial.mat");

                    // 球を格納するための親を作成
                    var parent = new GameObject("_Colliders_");
                    parent.transform.parent = cmp.gameObject.transform;
                    parent.hideFlags = HideFlags.DontSave;
                    parent.transform.localPosition = new Vector3();
                    parent.transform.localScale = Vector3.one;

                    int i = 0;
                    foreach (var cld in cmp.Colliders)
                    {
                        // 球を作成
                        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        sphere.transform.parent = parent.transform;
                        sphere.hideFlags = HideFlags.DontSave;
                        sphere.name = String.Format("Collider{0:000}", i);
                        if (mat != null)
                        {
                            var mesh = sphere.GetComponent<MeshRenderer>();
                            mesh.material = mat;
                            // mesh.material.color = Helper.GizmoColor(i);
                        }
                        var s = cld.Radius * 2;
                        sphere.transform.localScale = new Vector3(s, s, s);
                        sphere.transform.localPosition = cld.Offset;
                        i++;
                    }
                }
            }
        }

        [MenuItem("VRM/VRM Helper/Colliders/Commit Edited Colliders", true)]
        private static bool CommitEditedCollidersValidate()
        {
            return AreCollidersActive();
        }

        [MenuItem("VRM/VRM Helper/Colliders/Commit Edited Colliders", false)]
        private static void CommitEditedColliders()
        {
            DoCommitEditedColliders(false);
        }

        [MenuItem("VRM/VRM Helper/Colliders/Commit Edited Colliders (Symmetrically)", true)]
        private static bool CommitEditedCollidersSymmetricallyValidate()
        {
            return AreCollidersActive();
        }

        [MenuItem("VRM/VRM Helper/Colliders/Commit Edited Colliders (Symmetrically)", false)]
        private static void CommitEditedCollidersSymmetrically()
        {
            DoCommitEditedColliders(true);
        }

        [MenuItem("VRM/VRM Helper/Colliders/Rollback Colliders", true)]
        private static bool RollbackCollidersValidate()
        {
            return AreCollidersActive();
        }

        [MenuItem("VRM/VRM Helper/Colliders/Rollback Colliders", false)]
        private static void RollbackColliders()
        {
            RevertVisibility();
        }

        /// <summary>
        /// 球の座標とサイズをコライダーに反映し、編集を終了します。
        /// </summary>
        private static void DoCommitEditedColliders(bool symmetrically = false)
        {
            foreach (var cmp in Helper.FindAllComponentsInSelected<VRMSpringBoneColliderGroup>())
            {
                if (cmp.gameObject.name != "J_Bip_C_Head") continue;
                var parent = cmp.gameObject.transform.Find("_Colliders_");
                if (parent == null) continue;
                var clds = Enumerable.Empty<VRMSpringBoneColliderGroup.SphereCollider>();
                foreach (var sphere in parent.GetComponentsInChildren<SphereCollider>())
                {
                    var t = sphere.GetComponent<Transform>();
                    var radius = t.localScale.magnitude / Vector3.one.magnitude / 2;
                    var offset = t.localPosition;
                    var cld = new VRMSpringBoneColliderGroup.SphereCollider{ Radius = radius, Offset = offset };
                    clds = clds.Append(cld);
                    if (symmetrically && 0.001f <= Math.Abs(offset.x))
                    {
                        var offset2 = new Vector3(-offset.x, offset.y, offset.z);
                        var cld2 = new VRMSpringBoneColliderGroup.SphereCollider{ Radius = radius, Offset = offset2 };
                        clds = clds.Append(cld2);
                    }
                }
                if (clds.Count() == 0) throw new Exception("球が見つかりません");
                cmp.Colliders = clds.ToArray();
            }

            RevertVisibility();
        }

    }

}
