using UnityEngine;

namespace Members.CHG.Graphic.DynamicMeshCutter.Scripts.Ragdoll
{
    public class DynamicRagdollPart : MonoBehaviour
    {
        public string Name;
        public int Index;
        public Rigidbody Rigidbody;
        public CharacterJoint Joint;
        public Collider[] Colliders;
        public Vector3[] Vertices;
        public Color Color = Color.white;
    }
}

