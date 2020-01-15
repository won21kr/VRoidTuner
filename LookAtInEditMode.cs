#pragma warning disable 0414, 0649
using UnityEngine;
using VRM;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRoidTuner
{

    [ExecuteAlways, InitializeOnLoadAttribute]
    public class LookAtInEditMode : SceneViewRenderer
    {

        [System.Serializable]
        public class BlendShapeWeight
        {
            public int Index;

            [Range(0f, 100f)]
            public float Weight;
        }

        [SerializeField, Range(0, 20), Tooltip("視線が半分の角度を動くまでにかかるフレーム数")]
        int LookAtFrames = 2;

        [SerializeField, Range(0, 150), Tooltip("視線の変更角度が大きすぎるときに一時的にカメラを見るフレーム数")]
        int TransientFrames = 50;

        [SerializeField, Range(0f, 180f), Tooltip("この角度以上を「視線の変更角度が大きすぎる」と判断")]
        float SafeAngleGap = 115f;

        [SerializeField, Tooltip("表情を操作する顔のメッシュ")]
        SkinnedMeshRenderer faceMesh;

        [SerializeField]
        BlendShapeWeight[] angryShapeWeights;

        [SerializeField, Tooltip("見ようとしている座標をマークするためのデバッグ用オブジェクト")]
        Transform DebugMark;

        VRMLookAtHead LookAt;
        VRMLookAtBoneApplyer Applyer;
        Vector3 InterpolatedTarget;              // 補間された視線の先（現在の座標）
        Transform LastSelection;                 // 最後に選択していたオブジェクト（未選択時はシーンビューのカメラ）
        int SpentFramesSinceLastSelectionChanged = 0; // 最後に別のオブジェクトを選択してからの経過フレーム数


        Vector2 mousePosition = new Vector2();
        bool isAltDown;

        internal override void OnAwakeInEditor()
        {
            if (LookAt == null)
            {
                LookAt = GetComponent<VRMLookAtHead>();
                var animator = GetComponent<Animator>();
                if (animator == null) return;
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                LookAt.Head = head;
            }
            if (Applyer == null)
            {
                Applyer = GetComponent<VRMLookAtBoneApplyer>();
            }
            if (InterpolatedTarget == null)
            {
                InterpolatedTarget = SceneView.lastActiveSceneView.camera.gameObject.transform.position;
            }
            if (faceMesh == null)
            {
                foreach (var mesh in GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (mesh.gameObject.name == "Face") faceMesh = mesh;
                }
            }
            if (angryShapeWeights == null)
            {
                angryShapeWeights = new BlendShapeWeight[]{
                    new BlendShapeWeight{ Index=0, Weight=100 }
                };
            }
            // EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            // EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        // static LookAtInEditMode() {
        //     Debug.Log("Play");
        // }

        // void OnPlayModeChanged(PlayModeStateChange state)
        // {
        //     ResetModification();
        // }

        void ResetModification()
        {
            ApplyRotations(0, 0);
            for (var i=0; i<faceMesh.sharedMesh.blendShapeCount; i++)
            {
                faceMesh.SetBlendShapeWeight(i, 0);
            }
        }

        internal override void OnDestroyInEditor()
        {
            // EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            ResetModification();
        }

        // 頭を中心にSlerpします。
        Vector3 SlerpFromHead(Vector3 from, Vector3 to)
        {
            if (LookAtFrames <= 0) return to;
            // from=1, to=0 として0に漸近する指数関数に見立てる
            float r = Mathf.Pow(0.5f, 1f/LookAtFrames); // 1フレームあたりr倍
            var head = LookAt.Head.position;
            from -= head;
            to -= head;
            from.Normalize();
            to.Normalize();
            return head + Vector3.Slerp(to, from, r);
        }

        // 視線の変更角度が大きすぎないかを調べます。
        bool IsSafeAngleGap(Vector3 p1, Vector3 p2)
        {
            var head = LookAt.Head.position;
            return Vector3.Angle(p1-head, p2-head) <= SafeAngleGap;
        }

        bool IsBehindHead(Vector3 p)
        {
            return p.z <= LookAt.Head.position.z;
        }

        static Vector3 PerpendicularFoot(Ray r, Vector3 from)
        {
         return r.origin + Vector3.Project(from - r.origin, r.direction);
        }

        Vector3 WorldMousePosition(Camera camera)
        {
            var ray = camera.ScreenPointToRay(mousePosition);
            var head = LookAt.Head.position;
            float t = 0;
            Vector3 q = new Vector3();
            float r = (camera.transform.position - head).magnitude * 0.3f;
            if (MathUtils.IntersectRaySphere(ray, head, r, ref t, ref q)) return q;
            return PerpendicularFoot(ray, head);
        }

        internal override void OnFixedUpdateInEditor(SceneView view)
        {
            if (Event.current != null)
            {
                mousePosition = Event.current.mousePosition;
                mousePosition.y = view.camera.pixelHeight - mousePosition.y;
                isAltDown = Event.current.alt;
            }

            var camera = view.camera.gameObject.transform;
            ModifyBlendShape(camera);

            var selection = Selection.activeGameObject?.transform ?? camera;
            if (LastSelection == null) LastSelection = camera;
            if (LastSelection != selection)
            {
                SpentFramesSinceLastSelectionChanged = 0;
                var p1 = LastSelection.position;
                var p2 = selection.position;
                if (IsSafeAngleGap(p1, p2)) SpentFramesSinceLastSelectionChanged = TransientFrames;
            }
            var target = selection; // これから見ようとするオブジェクト
            var isTransitional = SpentFramesSinceLastSelectionChanged < TransientFrames; // 一時的にカメラを見ている
            if (selection == gameObject.transform ||
                selection.GetComponent<SkinnedMeshRenderer>() != null ||
                IsBehindHead(selection.position) ||
                isAltDown ||
                isTransitional)
            {
                target = camera;
            }
            var lookAtMouse = target == camera && !isAltDown && !isTransitional;
            var v = lookAtMouse ? WorldMousePosition(view.camera) : target.position;
            if (DebugMark != null) DebugMark.position = v;
            // TODO: vをFOVの範囲に吸着
            InterpolatedTarget = SlerpFromHead(InterpolatedTarget, v);
            LookAtImmediately(InterpolatedTarget);

            SpentFramesSinceLastSelectionChanged++;
            LastSelection = selection;
        }

        void LookAtImmediately(Vector3 target)
        {
            float yaw, pitch;
            LookAt.LookWorldPosition(target, out yaw, out pitch);
            ApplyRotations(yaw, pitch);
        }

        // from VRMLookAtBoneApplyer in UniVRM
        void ApplyRotations(float yaw, float pitch)
        {
            // horizontal
            float leftYaw, rightYaw;
            if (yaw < 0)
            {
                leftYaw = -Applyer.HorizontalOuter.Map(-yaw);
                rightYaw = -Applyer.HorizontalInner.Map(-yaw);
            }
            else
            {
                rightYaw = Applyer.HorizontalOuter.Map(yaw);
                leftYaw = Applyer.HorizontalInner.Map(yaw);
            }

            // vertical
            if (pitch < 0)
            {
                pitch = -Applyer.VerticalDown.Map(-pitch);
            }
            else
            {
                pitch = Applyer.VerticalUp.Map(pitch);
            }

            // Apply
            if (Applyer.LeftEye.Transform != null && Applyer.RightEye.Transform != null)
            {
                // 目に値を適用する
                Applyer.LeftEye.Transform.rotation = ExtractRotation(Applyer.LeftEye.InitialWorldMatrix) * Matrix4x4.identity.YawPitchRotation(leftYaw, pitch);
                Applyer.RightEye.Transform.rotation = ExtractRotation(Applyer.RightEye.InitialWorldMatrix) * Matrix4x4.identity.YawPitchRotation(rightYaw, pitch);
            }
        }

        // from UniGLTF
        // https://forum.unity.com/threads/how-to-assign-matrix4x4-to-transform.121966/
        static Quaternion ExtractRotation(Matrix4x4 matrix)
        {
            Vector3 forward;
            forward.x = matrix.m02;
            forward.y = matrix.m12;
            forward.z = matrix.m22;

            Vector3 upwards;
            upwards.x = matrix.m01;
            upwards.y = matrix.m11;
            upwards.z = matrix.m21;

            if (forward == Vector3.zero) return Quaternion.identity; // to suppress warnings
            return Quaternion.LookRotation(forward, upwards);
        }

        void ModifyBlendShape(Transform camera)
        {
            var localCamera = LookAt.Head.worldToLocalMatrix.MultiplyPoint(camera.position);
            float yaw, pitch;
            Matrix4x4.identity.CalcYawPitch(localCamera, out yaw, out pitch);
            var angry = Mathf.Clamp((-pitch - 10f) / 40f, 0f, 1f);

            foreach (var w in angryShapeWeights)
            {
                faceMesh.SetBlendShapeWeight(w.Index, w.Weight * angry);
            }
        }

    }

}
