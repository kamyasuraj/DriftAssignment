using UnityEngine;

namespace DriftAssignment.Core
{
    public interface IDamageable
    {
        bool IsInRange(Vector3 pointWorld);
        void ReceiveImpact(Vector3 contactPointWorld, Vector3 impulse, float impulseMagnitude);
    }
}
