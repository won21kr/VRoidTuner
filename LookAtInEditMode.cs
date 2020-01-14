#pragma warning disable 0414, 0649
using UnityEngine;
using VRM;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRoidTuner
{

    [ExecuteAlways]
    public class LookAtInEditMode : MonoBehaviour
    {

        VRMLookAtHead LookAt;
        VRMLookAtBoneApplyer Applyer;
        Vector3 InterpolatedTarget;
        Transform LastSelection;
        int FramesSinceLastSelectionChanged = 0;

        const float LookAtSpeed = 0.2f;
        const int FramesLookingCamera = 60;
        const float SafeAngleGap = 60f;

        void Awake()
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
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        Vector3 SlerpFromHead(Vector3 p1, Vector3 p2, float r)
        {
            var c = LookAt.Head.position;
            p1 -= c;
            p2 -= c;
            p1.Normalize();
            p2.Normalize();
            return c + Vector3.Slerp(p1, p2, r);
        }

        bool IsSafeAngleGap(Vector3 p1, Vector3 p2)
        {
            var c = LookAt.Head.position;
            return Vector3.Angle(p1-c, p2-c) <= SafeAngleGap;
        }

        bool IsBehindEyes(Vector3 p)
        {
            return p.z <= LookAt.Head.position.z;
        }

        void Tick()
        {
            if (Application.IsPlaying(gameObject)) return;
            var camera = SceneView.lastActiveSceneView.camera.gameObject.transform;
            var selection = Selection.activeGameObject?.transform ?? camera;
            if (LastSelection == null) LastSelection = camera;
            if (LastSelection != selection)
            {
                FramesSinceLastSelectionChanged = 0;
                var p1 = LastSelection.position;
                var p2 = selection.position;
                if (IsSafeAngleGap(p1, p2)) FramesSinceLastSelectionChanged = FramesLookingCamera;
            }
            var target = selection;
            if (IsBehindEyes(selection.position) || FramesSinceLastSelectionChanged < FramesLookingCamera)
            {
                target = camera;
            }
            var v = target.transform.position;
            InterpolatedTarget = SlerpFromHead(InterpolatedTarget, v, LookAtSpeed);
            float yaw;
            float pitch;
            LookAt.LookWorldPosition(InterpolatedTarget, out yaw, out pitch);
            ApplyRotations(yaw, pitch);

            FramesSinceLastSelectionChanged++;
            LastSelection = selection;
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

    }

}
