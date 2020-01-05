using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using VRM;

namespace VRMHelper
{

    public class VRMSetup : EditorWindow
    {

        [Serializable]
        internal class VRMSetupParams : ScriptableObject
        {

            internal bool UpdateWhenOffscreen = true;
            internal bool UseFirstPersonOnlyLayer = false;
            internal bool TurnOffReceivesShadowsOnFace = true;
            internal bool TurnOffReceivesShadowsOnHair = true;
            internal bool TurnOffReceivesShadowsOnBody = true;
            internal bool MakeEyeHighlightUnlit = true;
            internal bool DoVRMLookAtHeadAtLateUpdate = true;

            internal bool OverwriteDragForce = true;

            [SerializeField, Range(0, 2)] private float _DragForceValue = 1;
            public float DragForceValue
            {
                get { return _DragForceValue; }
                set { _DragForceValue = Mathf.Clamp(value, 0, 2); }
            }

            internal bool OverwriteStiffnessForce = false;

            [SerializeField, Range(0, 2)] private float _StiffnessForceValue = 1;
            public float StiffnessForceValue
            {
                get { return _StiffnessForceValue; }
                set { _StiffnessForceValue = Mathf.Clamp(value, 0, 2); }
            }

            internal bool AddHeadColliders = true;
            internal bool ShuffleSpringBoneGizmoColors = true;

        }

        private static Color[] _gizmoColors = new Color[]{
            new Color(0.0f, 0.0f, 0.0f, 1.0f),
            new Color(1.0f, 0.0f, 0.0f, 1.0f),
            new Color(1.0f, 0.5f, 0.0f, 1.0f),
            new Color(1.0f, 1.0f, 0.0f, 1.0f),
            new Color(0.5f, 1.0f, 0.0f, 1.0f),
            new Color(0.0f, 1.0f, 0.0f, 1.0f),
            new Color(0.0f, 1.0f, 0.5f, 1.0f),
            new Color(0.0f, 1.0f, 1.0f, 1.0f),
            new Color(0.0f, 0.5f, 1.0f, 1.0f),
            new Color(0.0f, 0.0f, 1.0f, 1.0f),
            new Color(0.5f, 0.0f, 1.0f, 1.0f),
            new Color(1.0f, 0.0f, 1.0f, 1.0f),
            new Color(1.0f, 0.0f, 0.5f, 1.0f),
            new Color(0.5f, 0.5f, 0.5f, 1.0f),
            new Color(1.0f, 1.0f, 1.0f, 1.0f),
        };

        private VRMSetupParams _params;
        private bool _selectedInHierarchy;
        private bool _selectedInProject;

        [MenuItem("VRM/VRM Helper/Open Setup Window")]
        private static void Create()
        {
            var window = GetWindow<VRMSetup>("VRM Setup");
            window.minSize = new Vector2(320, 320);
        }

        private void OnGUI()
        {
            if (_params == null)
            {
                _params = VRMSetupParams.CreateInstance<VRMSetupParams>();

                Selection.selectionChanged += () =>
                {
                    (_selectedInHierarchy, _selectedInProject) = Helper.IsVRMSelected();
                    Repaint();
                };
            }

            GUIStyle caption = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
            };
            var originalLabelWidth = EditorGUIUtility.labelWidth;
            GUI.enabled = true;

            if (!(_selectedInHierarchy || _selectedInProject))
            {
                EditorGUILayout.HelpBox("プロジェクトアセットまたはヒエラルキー内のVRMモデルプレハブを選択してください。", MessageType.Error);
            }

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.enabled = _selectedInHierarchy || _selectedInProject;
                EditorGUILayout.LabelField("「顔のメッシュが消える」対策", caption);
                EditorGUIUtility.labelWidth = 250;
                _params.UpdateWhenOffscreen = EditorGUILayout.Toggle("全メッシュの UpdateWhenOffscreen を On", _params.UpdateWhenOffscreen);
            }

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.enabled = _selectedInHierarchy;
                EditorGUILayout.LabelField("「VRで一人称視点にすると顔の裏面で視界が埋まる」対策", caption);
                EditorGUIUtility.labelWidth = 250;
                _params.UseFirstPersonOnlyLayer = EditorGUILayout.Toggle("FIRSTPERSON_ONLY_LAYER レイヤに設定", _params.UseFirstPersonOnlyLayer);
                EditorGUILayout.HelpBox(
                    "Face・Hair メッシュを FIRSTPERSON_ONLY_LAYER レイヤに設定します（定義されている場合のみ）。",
                    MessageType.Info);
                if (_selectedInProject) EditorGUILayout.HelpBox(
                    "ヒエラルキー中のオブジェクトにのみ適用されます。プロジェクトアセット中のプレハブに対する変更は保存されません。",
                    MessageType.Warning);
            }

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.enabled = _selectedInHierarchy || _selectedInProject;
                EditorGUILayout.LabelField("「顔に影が落ちてうっとうしい」対策", caption);
                EditorGUIUtility.labelWidth = 250;
                _params.TurnOffReceivesShadowsOnFace = EditorGUILayout.Toggle("Face の ReceivesShadows を Off", _params.TurnOffReceivesShadowsOnFace);
                _params.TurnOffReceivesShadowsOnHair = EditorGUILayout.Toggle("Hair の ReceivesShadows を Off", _params.TurnOffReceivesShadowsOnHair);
                _params.TurnOffReceivesShadowsOnBody = EditorGUILayout.Toggle("Body の ReceivesShadows を Off", _params.TurnOffReceivesShadowsOnBody);
            }

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.enabled = _selectedInHierarchy || _selectedInProject;
                EditorGUILayout.LabelField("「目のハイライトが暗い」対策", caption);
                EditorGUIUtility.labelWidth = 250;
                _params.MakeEyeHighlightUnlit = EditorGUILayout.Toggle("ハイライトのシェーダを Unlit に変更", _params.MakeEyeHighlightUnlit);
                EditorGUILayout.HelpBox(
                    "名前に \"EyeHighlight\" が含まれるマテリアルのシェーダを \"VRM/UnlitTransparent\" に変更します。",
                    MessageType.Info);
                if (_selectedInHierarchy) EditorGUILayout.HelpBox(
                    "プロジェクトアセット中のパラメータが変更されるため、この設定はプロジェクトを通じて共有されます。",
                    MessageType.Warning);
            }

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.enabled = _selectedInHierarchy || _selectedInProject;
                EditorGUILayout.LabelField("「カメラの方を見てくれない」対策", caption);
                EditorGUIUtility.labelWidth = 250;
                _params.DoVRMLookAtHeadAtLateUpdate = EditorGUILayout.Toggle("VRMLookAtHead を LateUpdate で行う", _params.DoVRMLookAtHeadAtLateUpdate);
            }

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.enabled = _selectedInProject;
                EditorGUILayout.LabelField("「揺れものが思い通りに揺れない」対策", caption);
                _params.OverwriteDragForce = EditorGUILayout.Toggle("DragForce を変更", _params.OverwriteDragForce);
                _params.DragForceValue = EditorGUILayout.FloatField("DragForce の値", _params.DragForceValue);
                _params.OverwriteStiffnessForce = EditorGUILayout.Toggle("StiffnessForce を変更", _params.OverwriteStiffnessForce);
                _params.StiffnessForceValue = EditorGUILayout.FloatField("StiffnessForce の値", _params.StiffnessForceValue);
                EditorGUILayout.HelpBox(
                    "髪と思しき（comment が空文字の）全 VRMSpringBone を対象にパラメータを上書き変更します。",
                    MessageType.Info);
                if (_selectedInHierarchy) EditorGUILayout.HelpBox(
                    "プロジェクトアセット中のプレハブにのみ適用されます。ヒエラルキー中のオブジェクトに対する変更は、開始時に破棄される場合があります。",
                    MessageType.Warning);
            }

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.enabled = _selectedInProject;
                EditorGUILayout.LabelField("「長い前髪が顔に埋まる」対策", caption);
                EditorGUIUtility.labelWidth = 250;
                _params.AddHeadColliders = EditorGUILayout.Toggle("J_Bip_C_Head にコライダーを追加", _params.AddHeadColliders);
                EditorGUILayout.HelpBox(
                    "J_Bip_C_Head の VRM Spring Bone Collider Group にコライダーを追加します。\n" +
                    "2つ以上のコライダーが既に設定されている場合は何も変更しません。",
                    MessageType.Info);
                if (_selectedInHierarchy) EditorGUILayout.HelpBox(
                    "プロジェクトアセット中のプレハブにのみ適用されます。ヒエラルキー中のオブジェクトに対する変更は、開始時に破棄される場合があります。",
                    MessageType.Warning);
            }

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.enabled = _selectedInHierarchy || _selectedInProject;
                EditorGUILayout.LabelField("その他", caption);
                EditorGUIUtility.labelWidth = 250;
                _params.ShuffleSpringBoneGizmoColors = EditorGUILayout.Toggle("VRMSpringBone のギズモ色をシャッフル", _params.ShuffleSpringBoneGizmoColors);
                if (_selectedInHierarchy) EditorGUILayout.HelpBox(
                    "プロジェクトアセット中のパラメータが変更されるため、この設定はプロジェクトを通じて共有されます。",
                    MessageType.Warning);
            }

            EditorGUIUtility.labelWidth = originalLabelWidth;
            GUI.enabled = true;

            using (new GUILayout.HorizontalScope())
            {
                GUI.enabled = _selectedInHierarchy || _selectedInProject;
                if (GUILayout.Button("適用"))
                {
                    ApplyOptimization();
                }
            }
        }

        private void ApplyOptimization()
        {
            ApplyMeshOptimization();

            // 「カメラの方を見てくれない」対策
            if (_params.DoVRMLookAtHeadAtLateUpdate)
            {
                foreach (var cmp in Helper.FindAllComponentsInSelected<VRMLookAtHead>())
                {
                    cmp.UpdateType = UpdateType.LateUpdate;
                }
            }

            // // 「リップシンクが効かない」対策
            // if (_params.DoOVRLipSyncAtLateUpdate)
            // {
            //     var cmps = Selection.GetFiltered(typeof(OVRLipSyncContextMorphTarget), SelectionMode.Deep);
            //     foreach (OVRLipSyncContextMorphTarget cmp in cmps) cmp.UpdateType = UpdateType.LateUpdate;
            // }

            ApplySpringBoneOptimization();
        }

        private void ApplyMeshOptimization()
        {
            var firstpersonOnlyLayer = LayerMask.NameToLayer("FIRSTPERSON_ONLY_LAYER");
            var vrmUnlitTransparentShader = Shader.Find("VRM/UnlitTransparent");

            var cmps = Helper.FindAllComponentsInSelected<SkinnedMeshRenderer>();
            foreach (var cmp in cmps)
            {
                var obj = cmp.gameObject;
                var isFace = obj.name == "Face";
                var isHair = obj.name.StartsWith("Hair");
                var isBody = obj.name == "Body";

                // 「顔のメッシュが消える」対策
                if (_params.UpdateWhenOffscreen) cmp.updateWhenOffscreen = true;

                // 「VRで一人称視点にすると顔の裏面で視界が埋まる」対策（ヒエラルキー中のオブジェクトにのみ適用可）
                if ((isFace || isHair) && 0 <= firstpersonOnlyLayer && _params.UseFirstPersonOnlyLayer)
                {
                    obj.layer = firstpersonOnlyLayer;
                }

                // 「顔に影が落ちてうっとうしい」対策
                if (isFace && _params.TurnOffReceivesShadowsOnFace) cmp.receiveShadows = false;
                if (isHair && _params.TurnOffReceivesShadowsOnHair) cmp.receiveShadows = false;
                if (isBody && _params.TurnOffReceivesShadowsOnBody) cmp.receiveShadows = false;

                // 「目のハイライトが暗い」対策
                if (isFace && vrmUnlitTransparentShader != null && _params.MakeEyeHighlightUnlit)
                {
                    foreach (var mat in cmp.sharedMaterials)
                    {
                        if (mat.name.Contains("EyeHighlight")) mat.shader = vrmUnlitTransparentShader;
                    }
                }
            }
        }

        private void ApplySpringBoneOptimization()
        {
            // 「揺れものが思い通りに揺れない」対策（プロジェクト中のプレハブにのみ適用可）
            foreach (var cmp in Helper.FindAllComponentsInSelected<VRMSpringBone>())
            {
                if (cmp.m_comment == "")
                {
                    if (_params.OverwriteDragForce) cmp.m_dragForce = _params.DragForceValue;
                    if (_params.OverwriteStiffnessForce) cmp.m_stiffnessForce = _params.StiffnessForceValue;
                }
                if (_params.ShuffleSpringBoneGizmoColors)
                {
                    var ser = new SerializedObject(cmp);
                    ser.Update();
                    ser.FindProperty("m_gizmoColor").colorValue = _gizmoColors[UnityEngine.Random.Range(0, _gizmoColors.Length)];
                    ser.ApplyModifiedProperties();
                }
            }

            // 「長い前髪が顔に埋まる」対策
            if (_params.AddHeadColliders)
            {
                foreach (var cmp in Helper.FindAllComponentsInSelected<VRMSpringBoneColliderGroup>())
                {
                    if (cmp.gameObject.name != "J_Bip_C_Head") continue;
                    if (cmp.Colliders.Length == 1)
                    {
                        // 制作者環境でのリファレンス値
                        var refDefaultOffset = new Vector3(0f, 0.09858859f, -0.01234177f); // VRoid Studio が出力した球
                        float refDefaultRadius = 0.09824938f;
                        var refSecondOffset = new Vector3(0f, 0.05687952f, 0.01470231f); // 顎寄り中央に追加した球
                        float refSecondRadius = 0.078f;
                        var refCheekOffset = new Vector3(0.02053911f, 0.0374347f, 0.04125598f); // +X側の頬寄りに追加した球
                        float refCheekRadius = 0.054f;

                        // 操作対象に唯一付与されている（VRoid Studio が出力したと思しき）球
                        var defaultRadius = cmp.Colliders[0].Radius;
                        var defaultOffset = cmp.Colliders[0].Offset;
                        var scaler = defaultRadius / refDefaultRadius; // リファレンス値に対するサイズ比
                        // TODO: 顔の縦横比等を考慮

                        // 顎寄り中央に追加する球
                        var secondRadius = refSecondRadius * scaler;
                        var secondOffset = defaultOffset + (refSecondOffset - refDefaultOffset) * scaler;

                        // 左右の頬寄りに追加する球
                        var cheekRadius = refCheekRadius * scaler;
                        var cheekOffsetL = defaultOffset + (refCheekOffset - refDefaultOffset) * scaler;
                        var cheekOffsetR = new Vector3(-cheekOffsetL.x, cheekOffsetL.y, cheekOffsetL.z);

                        cmp.Colliders = new VRMSpringBoneColliderGroup.SphereCollider[4]{
                            cmp.Colliders[0],
                            new VRMSpringBoneColliderGroup.SphereCollider{ Radius = secondRadius, Offset = secondOffset },
                            new VRMSpringBoneColliderGroup.SphereCollider{ Radius = cheekRadius, Offset = cheekOffsetL },
                            new VRMSpringBoneColliderGroup.SphereCollider{ Radius = cheekRadius, Offset = cheekOffsetR },
                        };
                    }
                }
            }
        }

    }

}
