using UnityEngine;

namespace DriftAssignment.Vehicle
{
    [DisallowMultipleComponent]
    public class WheelController : MonoBehaviour
    {
        [SerializeField] private WheelCollider _collider;
        [SerializeField] private Transform _visual;

        private void Reset()
        {
            _collider = GetComponent<WheelCollider>();
        }

        private void LateUpdate()
        {
            if (_collider == null || _visual == null) return;
            _collider.GetWorldPose(out var pos, out var rot);
            _visual.SetPositionAndRotation(pos, rot);
        }

        public void Bind(WheelCollider collider, Transform visual)
        {
            _collider = collider;
            _visual = visual;
        }
    }
}
