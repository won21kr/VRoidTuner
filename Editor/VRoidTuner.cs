using System;
using UnityEngine;
using UnityEditor;
using VRM;

namespace VRoidTuner
{

    public class VRoidTuner : EditorWindow
    {

        [Serializable]
        internal class VRoidTunerParams : ScriptableObject
        {

            internal bool UpdateWhenOffscreen = true;
            internal bool UseFirstPersonOnlyLayer = false;
            internal bool TurnOffReceivesShadowsOnFace = true;
            internal bool TurnOffReceivesShadowsOnHair = true;
            internal bool TurnOffReceivesShadowsOnBody = true;
            internal bool MakeEyeHighlightUnlit = true;
            internal bool DoVRMLookAtHeadAtLateUpdate = true;

            internal bool OverwriteDragForce = true;

            [SerializeField, Range(0, 2)] private float _DragForceOffset = .4f;
            public float DragForceOffset
            {
                get { return _DragForceOffset; }
                set { _DragForceOffset = Mathf.Clamp(value, 0, 2); }
            }

            [SerializeField, Range(0, 2)] private float _GravityPowerToDragForceRatio = .6f;
            public float GravityPowerToDragForceRatio
            {
                get { return _GravityPowerToDragForceRatio; }
                set { _GravityPowerToDragForceRatio = Mathf.Clamp(value, -2, 2); }
            }

            internal bool OverwriteStiffnessForce = false;

            [SerializeField, Range(0, 2)] private float _StiffnessForceOffset = 1f;
            public float StiffnessForceOffset
            {
                get { return _StiffnessForceOffset; }
                set { _StiffnessForceOffset = Mathf.Clamp(value, 0, 2); }
            }

            [SerializeField, Range(0, 2)] private float _GravityPowerToStiffnessForceRatio = -1f;
            public float GravityPowerToStiffnessForceRatio
            {
                get { return _GravityPowerToStiffnessForceRatio; }
                set { _GravityPowerToStiffnessForceRatio = Mathf.Clamp(value, -2, 2); }
            }


            internal bool AddHeadColliders = true;
            internal bool ShuffleSpringBoneGizmoColors = true;

            [SerializeField, Range(-3600, 3600)] private float _HairJointTwistAngle = 90f;
            public float HairJointTwistAngle
            {
                get { return _HairJointTwistAngle; }
                set { _HairJointTwistAngle = Mathf.Clamp(value, -3600, 3600); }
            }

        }

        static GUIStyle caption;
        static GUIStyle foldout;

        private VRoidTunerParams _params;
        private Vector2 _scrollPos = Vector2.zero;
        private bool _vrmSelectedInHierarchy;
        private bool _vrmSelectedInProject;
        private bool _hairJointSelected;

        [MenuItem("VRM/VRoidTuner/Open Setup Window")]
        private static void Create()
        {
            var window = GetWindow<VRoidTuner>("VRoidTuner");
            window.minSize = new Vector2(350, 350);
            window.selectionUpdated();
        }

        private void OnSelectionChange()
        {
            selectionUpdated();
        }

        private void selectionUpdated()
        {
            (_vrmSelectedInHierarchy, _vrmSelectedInProject) = Helper.IsVRMSelected();

            _hairJointSelected = false;
            foreach (var obj in Selection.GetFiltered<GameObject>(SelectionMode.TopLevel))
            {
                if (obj.name.StartsWith("HairJoint-")) _hairJointSelected = true;
            }

            Repaint();
        }

        class Section : IDisposable
        {
            IDisposable scope;
            public Section(string title, float labelWidth, bool enabled)
            {
                scope = new GUILayout.VerticalScope();
                GUI.enabled = enabled;
                if (caption == null)
                {
                    caption = new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontStyle = FontStyle.Bold,
                    };
                }
                EditorGUILayout.LabelField(title, caption);
                EditorGUIUtility.labelWidth = labelWidth;
                EditorGUI.indentLevel++;
            }
            public void Dispose()
            {
                EditorGUI.indentLevel--;
                scope.Dispose();
                EditorGUILayout.Separator();
            }
        }

        class Group
        {
            bool isOpen = true;

            public void Foldout(string title, Action a)
            {
                GUI.enabled = true;
                if (foldout == null)
                {
                    foldout = new GUIStyle(EditorStyles.foldout);
                    foldout.fontStyle = FontStyle.Bold;
                }
                isOpen = EditorGUILayout.Foldout(isOpen, title, foldout);
                if (isOpen)
                {
                    EditorGUI.indentLevel++;
                    a();
                    EditorGUILayout.Separator();
                    EditorGUI.indentLevel--;
                }
            }
        }

        private Group SetupGroup = new Group();
        private Group EtcGroup = new Group();

        private void OnGUI()
        {
            selectionUpdated();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (_params == null)
            {
                _params = VRoidTunerParams.CreateInstance<VRoidTunerParams>();
            }

            var originalLabelWidth = EditorGUIUtility.labelWidth;

            SetupGroup.Foldout("セットアップ", () =>
            {
                if (!(_vrmSelectedInHierarchy || _vrmSelectedInProject))
                {
                    EditorGUILayout.HelpBox("プロジェクトアセットまたはヒエラルキー内のVRMモデルプレハブを選択してください。", MessageType.Error);
                    EditorGUILayout.Separator();
                }

                using (new Section("「顔のメッシュが消える」対策", 270, _vrmSelectedInHierarchy || _vrmSelectedInProject))
                {
                    _params.UpdateWhenOffscreen = EditorGUILayout.Toggle("全メッシュの UpdateWhenOffscreen を On", _params.UpdateWhenOffscreen);
                }

                using (new Section("「VRで一人称視点にすると顔の裏面で視界が埋まる」対策", 270, _vrmSelectedInHierarchy))
                {
                    _params.UseFirstPersonOnlyLayer = EditorGUILayout.Toggle("FIRSTPERSON_ONLY_LAYER レイヤに設定", _params.UseFirstPersonOnlyLayer);
                    EditorGUILayout.HelpBox(
                        "Face・Hair メッシュを FIRSTPERSON_ONLY_LAYER レイヤに設定します（定義されている場合のみ）。",
                        MessageType.Info);
                    if (_vrmSelectedInProject) EditorGUILayout.HelpBox(
                        "ヒエラルキー中のオブジェクトにのみ適用されます。プロジェクトアセット中のプレハブに対する変更は保存されません。",
                        MessageType.Warning);
                }

                using (new Section("「顔に影が落ちてうっとうしい」対策", 270, _vrmSelectedInHierarchy || _vrmSelectedInProject))
                {
                    _params.TurnOffReceivesShadowsOnFace = EditorGUILayout.Toggle("Face の ReceivesShadows を Off", _params.TurnOffReceivesShadowsOnFace);
                    _params.TurnOffReceivesShadowsOnHair = EditorGUILayout.Toggle("Hair の ReceivesShadows を Off", _params.TurnOffReceivesShadowsOnHair);
                    _params.TurnOffReceivesShadowsOnBody = EditorGUILayout.Toggle("Body の ReceivesShadows を Off", _params.TurnOffReceivesShadowsOnBody);
                }

                using (new Section("「目のハイライトが暗い」対策", 270, _vrmSelectedInHierarchy || _vrmSelectedInProject))
                {
                    _params.MakeEyeHighlightUnlit = EditorGUILayout.Toggle("ハイライトのシェーダを Unlit に変更", _params.MakeEyeHighlightUnlit);
                    EditorGUILayout.HelpBox(
                        "名前に \"EyeHighlight\" が含まれるマテリアルのシェーダを \"VRM/UnlitTransparent\" に変更します。",
                        MessageType.Info);
                    if (_vrmSelectedInHierarchy) EditorGUILayout.HelpBox(
                        "プロジェクトアセット中のパラメータが変更されるため、この設定はプロジェクトを通じて共有されます。",
                        MessageType.Warning);
                }

                using (new Section("「カメラの方を見てくれない」対策", 270, _vrmSelectedInHierarchy || _vrmSelectedInProject))
                {
                    _params.DoVRMLookAtHeadAtLateUpdate = EditorGUILayout.Toggle("VRMLookAtHead を LateUpdate で行う", _params.DoVRMLookAtHeadAtLateUpdate);
                }

                using (new Section("「揺れものが思い通りに揺れない」対策", 270, _vrmSelectedInHierarchy || _vrmSelectedInProject))
                {
                    _params.OverwriteDragForce = EditorGUILayout.Toggle("DragForce（抵抗力）を変更", _params.OverwriteDragForce);
                    _params.DragForceOffset = EditorGUILayout.FloatField("        基準値", _params.DragForceOffset);
                    _params.GravityPowerToDragForceRatio = EditorGUILayout.FloatField("        + GravityPower ×", _params.GravityPowerToDragForceRatio);
                    _params.OverwriteStiffnessForce = EditorGUILayout.Toggle("StiffnessForce（復元力）を変更 ※非推奨", _params.OverwriteStiffnessForce);
                    _params.StiffnessForceOffset = EditorGUILayout.FloatField("        基準値", _params.StiffnessForceOffset);
                    _params.GravityPowerToStiffnessForceRatio = EditorGUILayout.FloatField("        + GravityPower ×", _params.GravityPowerToStiffnessForceRatio);
                    EditorGUILayout.HelpBox(
                        "髪と思しき（comment が空文字の）全 VRMSpringBone を対象にパラメータを上書き変更します。",
                        MessageType.Info);
                    if (_vrmSelectedInHierarchy) EditorGUILayout.HelpBox(
                        "ヒエラルキー中のオブジェクトに対する変更は、開始時に破棄される場合があります。これを避けるには、プレハブに上書き保存してください。",
                        MessageType.Warning);
                }

                using (new Section("「長い前髪が顔に埋まる」対策", 270, _vrmSelectedInHierarchy || _vrmSelectedInProject))
                {
                    _params.AddHeadColliders = EditorGUILayout.Toggle("J_Bip_C_Head にコライダーを追加", _params.AddHeadColliders);
                    EditorGUILayout.HelpBox(
                        "J_Bip_C_Head の VRM Spring Bone Collider Group にコライダーを追加します。\n" +
                        "2つ以上のコライダーが既に設定されている場合は何も変更しません。",
                        MessageType.Info);
                    if (_vrmSelectedInHierarchy) EditorGUILayout.HelpBox(
                        "ヒエラルキー中のオブジェクトに対する変更は、開始時に破棄される場合があります。これを避けるには、プレハブに上書き保存してください。",
                        MessageType.Warning);
                }

                using (new Section("その他の設定", 270, _vrmSelectedInHierarchy || _vrmSelectedInProject))
                {
                    _params.ShuffleSpringBoneGizmoColors = EditorGUILayout.Toggle("VRMSpringBone のギズモ色をシャッフル", _params.ShuffleSpringBoneGizmoColors);
                    if (_vrmSelectedInHierarchy) EditorGUILayout.HelpBox(
                        "プロジェクトアセット中のパラメータが変更されるため、この設定はプロジェクトを通じて共有されます。",
                        MessageType.Warning);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUI.enabled = _vrmSelectedInHierarchy || _vrmSelectedInProject;
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("適用")) ApplyOptimization();
                }
            });

            EtcGroup.Foldout("その他の操作", () =>
            {

                using (new Section("顔コライダー", 270, FaceColliderTuner.IsVRMPrefabMode()))
                {
                    if (!FaceColliderTuner.IsVRMPrefabMode())
                    {
                        EditorGUILayout.HelpBox("プレハブモードでVRMモデルを編集状態にしてください。", MessageType.Error);
                        EditorGUILayout.Separator();
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        GUI.enabled = FaceColliderTuner.IsVRMPrefabMode() && !FaceColliderTuner.AreCollidersActive();
                        if (GUILayout.Button("編集開始")) FaceColliderTuner.BeginToEditHeadColliders();

                        GUI.enabled = FaceColliderTuner.AreCollidersActive();
                        if (GUILayout.Button("完了")) FaceColliderTuner.CommitEditedColliders(false);
                        if (GUILayout.Button("完了(鏡像化)")) FaceColliderTuner.CommitEditedColliders(true);
                        if (GUILayout.Button("破棄")) FaceColliderTuner.RollbackColliders();
                    }
                }

                using (new Section("揺れもの", 270, _vrmSelectedInHierarchy))
                {
                    if (!_vrmSelectedInHierarchy)
                    {
                        EditorGUILayout.HelpBox("ヒエラルキー内のVRMモデルプレハブを選択してください。", MessageType.Error);
                        EditorGUILayout.Separator();
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUI.enabled = _vrmSelectedInHierarchy;
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("揺れものギズモ表示")) HairTuner.ShowAllVRMSpringBoneGizmos(true);
                        if (GUILayout.Button("非表示")) HairTuner.ShowAllVRMSpringBoneGizmos(false);
                    }
                }

                using (new Section("髪束をひねる", 270, _hairJointSelected))
                {
                    if (!_hairJointSelected)
                    {
                        EditorGUILayout.HelpBox("ヒエラルキー内の髪ボーン（HairJoint-*）を選択してください。", MessageType.Error);
                        EditorGUILayout.Separator();
                    }

                    using (new GUILayout.VerticalScope())
                    {
                        GUI.enabled = _hairJointSelected;
                        EditorGUIUtility.labelWidth = 270;
                        _params.HairJointTwistAngle = EditorGUILayout.FloatField("角度", _params.HairJointTwistAngle);
                    }
                    EditorGUILayout.Separator();

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("適用")) HairTuner.TwistHairJoint(_params.HairJointTwistAngle);
                    }
                }
            });

            EditorGUILayout.EndScrollView();
            EditorGUIUtility.labelWidth = originalLabelWidth;
            GUI.enabled = true;
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
                    EditorUtility.SetDirty(cmp);
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
            var firstpersonOnlyLayer = LayerMask.NameToLayer("FIRSTPERSON_ONLY_LAYER"); // == 9
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

                EditorUtility.SetDirty(cmp);
                EditorUtility.SetDirty(obj);
            }
        }

        private void ApplySpringBoneOptimization()
        {
            // 「揺れものが思い通りに揺れない」対策（プロジェクト中のプレハブにのみ適用可）
            foreach (var cmp in Helper.FindAllComponentsInSelected<VRMSpringBone>())
            {
                if (cmp.m_comment == "")
                {
                    var g = cmp.m_gravityPower;
                    if (_params.OverwriteDragForce) cmp.m_dragForce = _params.DragForceOffset + g * _params.GravityPowerToDragForceRatio;
                    if (_params.OverwriteStiffnessForce) cmp.m_stiffnessForce = _params.StiffnessForceOffset + g * _params.GravityPowerToStiffnessForceRatio;
                }
                if (_params.ShuffleSpringBoneGizmoColors)
                {
                    var ser = new SerializedObject(cmp);
                    ser.Update();
                    ser.FindProperty("m_gizmoColor").colorValue = Helper.GizmoColor();
                    ser.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(cmp);
                EditorUtility.SetDirty(cmp.gameObject);
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
                        var refNoseOffset = new Vector3(0f, 0.0752f, 0.0155f); // 鼻寄り中央に追加した球
                        float refNoseRadius = 0.156f / 2;
                        var refForeheadOffset = new Vector3(0.0101f, 0.1073f, 0.0133f); // +X側の額寄りに追加した球
                        float refForeheadRadius = 0.15887f / 2;
                        var refCheekOffset = new Vector3(0.02053911f, 0.0374347f, 0.04125598f); // +X側の頬寄りに追加した球
                        float refCheekRadius = 0.054f;

                        // 操作対象に唯一付与されている（VRoid Studio が出力したと思しき）球
                        var defaultRadius = cmp.Colliders[0].Radius;
                        var defaultOffset = cmp.Colliders[0].Offset;
                        var scaler = defaultRadius / refDefaultRadius; // リファレンス値に対するサイズ比
                        // TODO: 顔の縦横比等を考慮

                        // 鼻寄り中央に追加する球
                        var noseRadius = refNoseRadius * scaler;
                        var noseOffset = defaultOffset + (refNoseOffset - refDefaultOffset) * scaler;

                        // 左右の額寄りに追加する球
                        var foreheadRadius = refForeheadRadius * scaler;
                        var foreheadOffsetL = defaultOffset + (refForeheadOffset - refDefaultOffset) * scaler;
                        var foreheadOffsetR = new Vector3(-foreheadOffsetL.x, foreheadOffsetL.y, foreheadOffsetL.z);

                        // 左右の頬寄りに追加する球
                        var cheekRadius = refCheekRadius * scaler;
                        var cheekOffsetL = defaultOffset + (refCheekOffset - refDefaultOffset) * scaler;
                        var cheekOffsetR = new Vector3(-cheekOffsetL.x, cheekOffsetL.y, cheekOffsetL.z);

                        cmp.Colliders = new VRMSpringBoneColliderGroup.SphereCollider[6]{
                            cmp.Colliders[0],
                            new VRMSpringBoneColliderGroup.SphereCollider{ Radius = noseRadius, Offset = noseOffset },
                            new VRMSpringBoneColliderGroup.SphereCollider{ Radius = foreheadRadius, Offset = foreheadOffsetL },
                            new VRMSpringBoneColliderGroup.SphereCollider{ Radius = foreheadRadius, Offset = foreheadOffsetR },
                            new VRMSpringBoneColliderGroup.SphereCollider{ Radius = cheekRadius, Offset = cheekOffsetL },
                            new VRMSpringBoneColliderGroup.SphereCollider{ Radius = cheekRadius, Offset = cheekOffsetR },
                        };
                        EditorUtility.SetDirty(cmp);
                        EditorUtility.SetDirty(cmp.gameObject);
                    }
                }
            }
        }

    }

}
