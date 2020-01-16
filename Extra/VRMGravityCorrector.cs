using UnityEngine;
using VRM;

namespace VRoidTuner
{

    public class VRMGravityCorrector : MonoBehaviour
    {

        VRMSpringBone[] springBones;

        void Start()
        {
            springBones = GetComponentsInChildren<VRMSpringBone>();
        }

        void Update()
        {
            var g = Physics.gravity / 9.81f;
            foreach (var bone in springBones) bone.m_gravityDir = g;
            // TODO: オリジナルの m_gravityDir を考慮
        }
    }

}
