using System;
using UnityEngine;
using UnityEditor;
using VRM;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;

namespace VRMHelper
{

    public class ColliderHelper : EditorWindow
    {

        /// <summary>
        /// プレハブモードでVRMモデルを編集中であるときtrueを返します。
        /// </summary>
        private static GameObject GetPrefabRootVRM()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null) return null;
            var result = stage.prefabContentsRoot;
            if (result.GetComponent<VRMMeta>() == null) return null;
            return result;
        }

        private static GameObject FindByName(string name)
        {
            var root = GetPrefabRootVRM();
            if (root == null) return null;
            foreach (var cmp in root.GetComponentsInChildren<Transform>(true))
            {
                if (cmp.gameObject.name == name) return cmp.gameObject;
            }
            return null;
        }

        /// <summary>
        /// 球が作成済みで、かつ表示状態であるときtrueを返します。
        /// </summary>
        private static bool AreCollidersActive()
        {
            var root = GetPrefabRootVRM();
            if (root == null) return false;
            var clds = FindByName("_Colliders_");
            if (clds == null) return false;
            return clds.activeInHierarchy;
        }

        /// <summary>
        /// 編集を終了するため、オブジェクトの表示状態を通常の状態に戻します。
        /// </summary>
        private static void RevertVisibility(GameObject root = null)
        {
            if (root == null) root = GetPrefabRootVRM();
            if (root == null) return;

            // 隠してあったメッシュを再表示
            foreach (var cmp in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var obj = cmp.gameObject;
                var isFace = obj.name == "Face";
                var isHair = obj.name.StartsWith("Hair");
                var isBody = obj.name == "Body";
                if (isFace || isHair || isBody) obj.SetActive(true);
                if (obj.name == "_DummyFace_") obj.SetActive(false); // 半透明の顔は非表示
                EditorUtility.SetDirty(obj);
            }

            // 球を非表示
            var clds = FindByName("_Colliders_");
            if (clds != null) clds.SetActive(false);

            EditorUtility.SetDirty(root);
        }

        [MenuItem("VRM/VRM Helper/Colliders/Begin To Edit Head Colliders (In Prefab Mode)", true)]
        private static bool BeginToEditHeadCollidersValid()
        {
            return GetPrefabRootVRM() != null && !AreCollidersActive();
        }

        private static bool IsInitialized = false;

        private static void onPrefabStageOpenedOrClosing(PrefabStage stage)
        {
            var root = stage.prefabContentsRoot;
            if (root.GetComponent<VRMMeta>() == null) return;
            RevertVisibility(root);
        }

        private static void Init()
        {
            if (IsInitialized) return;
            PrefabStage.prefabStageClosing += onPrefabStageOpenedOrClosing;
            PrefabStage.prefabStageOpened += onPrefabStageOpenedOrClosing;
            IsInitialized = true;
        }

        [MenuItem("VRM/VRM Helper/Colliders/Begin To Edit Head Colliders (In Prefab Mode)", false)]
        private static void BeginToEditHeadColliders()
        {
            Init();

            var root = GetPrefabRootVRM();
            if (root == null) return;

            var head = FindByName("J_Bip_C_Head");
            if (head == null) throw new Exception("J_Bip_C_Head が見つかりません");

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null) return;

            // 半透明の顔が既にある場合は表示
            var dummyFace = FindByName("_DummyFace_");
            if (dummyFace != null) dummyFace.SetActive(true);

            // 全メッシュを隠し、半透明の顔を作成
            foreach (var cmp in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var obj = cmp.gameObject;
                var isFace = obj.name == "Face";
                var isHair = obj.name.StartsWith("Hair");
                var isBody = obj.name == "Body";
                if (isFace && dummyFace == null)
                {
                    // 顔を複製
                    dummyFace = Instantiate(obj);
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

            var clds = FindByName("_Colliders_");
            if (clds != null)
            {
                // 球を再表示
                clds.SetActive(true);
            }
            else
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VRMHelper/Materials/VRMHelperGizmoMaterial.mat");

                // 球を格納するための親を作成
                var parent = new GameObject("_Colliders_");
                parent.transform.parent = head.transform;
                parent.hideFlags = HideFlags.DontSave;
                parent.transform.localPosition = new Vector3();
                parent.transform.localScale = Vector3.one;

                int i = 0;
                var cgrp = head.GetComponent<VRMSpringBoneColliderGroup>();
                if (cgrp == null) throw new Exception("J_Bip_C_Head に VRMSpringBoneColliderGroup がアタッチされていません");
                foreach (var cld in cgrp.Colliders)
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
            var root = GetPrefabRootVRM();
            if (root == null) return;

            var head = FindByName("J_Bip_C_Head");
            if (head == null) throw new Exception("J_Bip_C_Head が見つかりません");
            var cgrp = head.GetComponent<VRMSpringBoneColliderGroup>();
            if (cgrp == null) throw new Exception("J_Bip_C_Head に VRMSpringBoneColliderGroup がアタッチされていません");

            var clds = FindByName("_Colliders_");
            if (clds == null) throw new Exception("_Colliders_ が見つかりません");

            var result = Enumerable.Empty<VRMSpringBoneColliderGroup.SphereCollider>();
            foreach (var cld in clds.GetComponentsInChildren<SphereCollider>())
            {
                var t = cld.GetComponent<Transform>();
                var radius = t.localScale.magnitude / Vector3.one.magnitude / 2;
                var offset = t.localPosition;
                var c = new VRMSpringBoneColliderGroup.SphereCollider{ Radius = radius, Offset = offset };
                result = result.Append(c);
                if (symmetrically && 0.001f <= Math.Abs(offset.x))
                {
                    var offset2 = new Vector3(-offset.x, offset.y, offset.z);
                    var c2 = new VRMSpringBoneColliderGroup.SphereCollider{ Radius = radius, Offset = offset2 };
                    result = result.Append(c2);
                }
            }
            if (result.Count() == 0) throw new Exception("球が見つかりません");
            cgrp.Colliders = result.ToArray();

            RevertVisibility();
            EditorUtility.SetDirty(head);
            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();
        }

    }

}
