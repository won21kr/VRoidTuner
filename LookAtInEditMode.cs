#pragma warning disable 0414, 0649
using UnityEngine;
using VRM;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRoidTuner
{

    [ExecuteInEditMode]
    public class LookAtInEditMode : MonoBehaviour
    {

        VRMLookAtHead LookAt;
        VRMLookAtBoneApplyer Applyer;
        GameObject LookAtTarget;

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
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        void OnSelectionChanged()
        {
            LookAtTarget = Selection.activeGameObject;
        }

        void OnRenderObject()
        {
            if (Application.IsPlaying(gameObject)) return;
            var target = LookAtTarget;
            if (target == null) target = SceneView.lastActiveSceneView.camera.gameObject;
            float yaw;
            float pitch;
            LookAt.LookWorldPosition(target.transform.position, out yaw, out pitch);
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

    }

}
